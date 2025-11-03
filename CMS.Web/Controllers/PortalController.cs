using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Web.Data;
using CMS.Web.Models;
using CMS.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace CMS.Web.Controllers
{
    /// <summary>
    /// This controller handles portal functionality for local development and testing.
    /// It can also serve as a fallback if the external backend is unavailable.
    /// </summary>
    [ApiController]
    [Route("api/portal")]
    public class PortalController : ControllerBase
    {
        private readonly CmsDbContext _context;
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<PortalController> _logger;
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com";

        public PortalController(
            CmsDbContext context, 
            IAuthService authService, 
            IJwtService jwtService,
            ILogger<PortalController> logger,
            HttpClient httpClient)
        {
            _context = context;
            _authService = authService;
            _jwtService = jwtService;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Portal login endpoint - accepts any valid user credentials for portal access
        /// </summary>
        [HttpPost("admin-login")]
        public async Task<IActionResult> PortalLogin([FromBody] AdminLoginRequest loginRequest)
        {
            try
            {
                _logger.LogInformation("Portal login attempt for email: {Email}", loginRequest.Email);
                
                // Try external API first to avoid local DB column case issues
                try 
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    var externalPayload = new { email = loginRequest.Email, password = loginRequest.Password };
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(externalPayload);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    
                    // Try user login endpoint for portal access (any valid user can access portal)
                    _logger.LogInformation("Calling external API: {Url} with email: {Email}", $"{API_BASE_URL}/api/auth/login", loginRequest.Email);
                    var externalResponse = await _httpClient.PostAsync($"{API_BASE_URL}/api/auth/login", content);
                    
                    var responseContent = await externalResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("External API response status: {Status}, Content: {Content}", externalResponse.StatusCode, responseContent);
                    
                    if (externalResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("External API login successful for: {Email}", loginRequest.Email);
                        return Ok(System.Text.Json.JsonSerializer.Deserialize<object>(responseContent));
                    }
                    
                    // Parse the error message from the API response
                    try 
                    {
                        using var document = System.Text.Json.JsonDocument.Parse(responseContent);
                        var errorMessage = document.RootElement.TryGetProperty("message", out var messageProp) 
                            ? messageProp.GetString() ?? "Login failed"
                            : "Login failed";
                        
                        _logger.LogWarning("External API login failed for {Email}: {Status} - {Message}", loginRequest.Email, externalResponse.StatusCode, errorMessage);
                        
                        // Return more specific error message
                        return Unauthorized(new { message = errorMessage });
                    }
                    catch 
                    {
                        _logger.LogWarning("External API login failed for {Email}: {Status} - {Content}", loginRequest.Email, externalResponse.StatusCode, responseContent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("External API unavailable: {Message}", ex.Message);
                }

                _logger.LogInformation("All login attempts failed");
                return Unauthorized(new { message = "Invalid credentials. Please check your email and password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during portal login for email: {LoginEmail}", loginRequest.Email);
                return StatusCode(500, new { message = "Login failed", details = "Please try again", error = ex.Message });
            }
        }

        /// <summary>
        /// Get current user details
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetUserDetails()
        {
            try
            {
                // Extract user email from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userEmail = _jwtService.GetEmailFromToken(token);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Try to get user from local database first
                var localUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (localUser != null)
                {
                    switch (localUser.Role)
                    {
                        case "Dependent":
                            // Dependents only see their own profile
                            return Ok(localUser);
                        
                        case "Funder":
                            // Funders see their own profile and basic info about their dependents
                            var dependentIds = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id) // Using BlockedBy temporarily as relationship
                                .Select(u => u.Id)
                                .ToListAsync();
                            return Ok(new
                            {
                                user = localUser,
                                dependentCount = dependentIds.Count,
                                hasActiveAccounts = await _context.Set<Account>()
                                    .AnyAsync(a => a.UserId.HasValue && dependentIds.Contains(a.UserId.Value))
                            });
                        
                        case "Caregiver":
                            // Caregivers see their profile and assigned patients/cases
                            var assignedDependents = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id) // Using BlockedBy temporarily as relationship
                                .Select(u => new { u.Id, u.FirstName, u.Surname })
                                .ToListAsync();
                            return Ok(new
                            {
                                user = localUser,
                                assignedDependents = assignedDependents
                            });
                        
                        default:
                            return Ok(localUser);
                    }
                }

                // If not found locally, try external API
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/portal/me");
                    if (response.IsSuccessStatusCode)
                    {
                        var userJson = await response.Content.ReadAsStringAsync();
                        var user = JsonSerializer.Deserialize<User>(userJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (user != null)
                        {
                            return Ok(user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                }

                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user details: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] User userUpdate)
        {
            try
            {
                // Extract user email from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userEmail = _jwtService.GetEmailFromToken(token);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Try to update in local database first
                var localUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (localUser != null)
                {
                    // Role-based profile update restrictions
                    switch (localUser.Role)
                    {
                        case "Dependent":
                            // Dependents can only update their basic info and contact details
                            break;
                        case "Funder":
                            // Funders can update their profile and manage dependent relationships
                            break;
                        case "Caregiver":
                            // Caregivers can update their profile and manage patient assignments
                            break;
                        default:
                            break;
                    }
                    
                    // Common profile updates for all roles
                    localUser.FirstName = userUpdate.FirstName ?? localUser.FirstName;
                    localUser.MiddleName = userUpdate.MiddleName ?? localUser.MiddleName;
                    localUser.Surname = userUpdate.Surname ?? localUser.Surname;
                    localUser.PhoneNumber = userUpdate.PhoneNumber ?? localUser.PhoneNumber;
                    localUser.IdNumber = userUpdate.IdNumber ?? localUser.IdNumber;
                    localUser.Relation = userUpdate.Relation ?? localUser.Relation;
                    localUser.PostalAddressLine1 = userUpdate.PostalAddressLine1 ?? localUser.PostalAddressLine1;
                    localUser.PostalAddressLine2 = userUpdate.PostalAddressLine2 ?? localUser.PostalAddressLine2;
                    localUser.PostalCity = userUpdate.PostalCity ?? localUser.PostalCity;
                    localUser.PostalProvince = userUpdate.PostalProvince ?? localUser.PostalProvince;
                    localUser.PostalCode = userUpdate.PostalCode ?? localUser.PostalCode;
                    localUser.HomeAddressLine1 = userUpdate.HomeAddressLine1 ?? localUser.HomeAddressLine1;
                    localUser.HomeAddressLine2 = userUpdate.HomeAddressLine2 ?? localUser.HomeAddressLine2;
                    localUser.HomeCity = userUpdate.HomeCity ?? localUser.HomeCity;
                    localUser.HomeProvince = userUpdate.HomeProvince ?? localUser.HomeProvince;
                    localUser.HomeCode = userUpdate.HomeCode ?? localUser.HomeCode;
                    localUser.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Profile updated successfully", user = localUser });
                }

                // If not found locally, try external API
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var jsonContent = JsonSerializer.Serialize(userUpdate);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.PutAsync($"{API_BASE_URL}/api/portal/me", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<object>(resultJson);
                        return Ok(result);
                    }
                    else
                    {
                        var errorJson = await response.Content.ReadAsStringAsync();
                        return BadRequest(new { message = "Failed to update profile", error = errorJson });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                    return StatusCode(500, new { message = "Failed to update profile", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user profile: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user accounts
        /// </summary>
        [HttpGet("me/accounts")]
        public async Task<IActionResult> GetUserAccounts()
        {
            try
            {
                // Extract user email from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userEmail = _jwtService.GetEmailFromToken(token);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Try to get user ID from local database first
                var localUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (localUser != null)
                {
                    var accountsQuery = _context.Set<Account>().AsQueryable();

                    switch (localUser.Role)
                    {
                        case "Dependent":
                            // Dependents only see their own accounts
                            accountsQuery = accountsQuery.Where(a => a.UserId == localUser.Id);
                            break;
                        
                        case "Funder":
                            // Funders see their accounts and their dependents' accounts
                            var dependentIds = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id) // Using BlockedBy temporarily as relationship
                                .Select(u => u.Id)
                                .ToListAsync();
                            accountsQuery = accountsQuery.Where(a => 
                                a.UserId == localUser.Id || (a.UserId.HasValue && dependentIds.Contains(a.UserId.Value)));
                            break;
                        
                        case "Caregiver":
                            // Caregivers see accounts of assigned patients
                            var patientIds = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id) // Using BlockedBy temporarily as relationship
                                .Select(u => u.Id)
                                .ToListAsync();
                            accountsQuery = accountsQuery.Where(a => a.UserId.HasValue && patientIds.Contains(a.UserId.Value));
                            break;
                        
                        default:
                            // Admin sees all accounts
                            break;
                    }

                    var localAccounts = await accountsQuery.ToListAsync();

                    if (localAccounts.Any())
                    {
                        return Ok(localAccounts);
                    }
                }

                // Try external API
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/portal/me/accounts");
                    if (response.IsSuccessStatusCode)
                    {
                        var accountsJson = await response.Content.ReadAsStringAsync();
                        var accounts = JsonSerializer.Deserialize<List<Account>>(accountsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        return Ok(accounts ?? new List<Account>());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                }

                // Return empty list if no accounts found
                return Ok(new List<Account>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user accounts: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user transactions
        /// </summary>
        [HttpGet("me/transactions")]
        public async Task<IActionResult> GetUserTransactions([FromQuery] string? type = null, [FromQuery] int limit = 50)
        {
            try
            {
                // Extract user email from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userEmail = _jwtService.GetEmailFromToken(token);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Get user for role check
                var localUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (localUser != null)
                {
                    var transactionsQuery = _context.Set<Transaction>().AsQueryable();

                    switch (localUser.Role)
                    {
                        case "Dependent":
                            // Dependents only see their own transactions
                            transactionsQuery = transactionsQuery
                                .Where(t => t.Account != null && t.Account.UserId == localUser.Id);
                            break;
                        
                        case "Funder":
                            // Funders see their transactions and their dependents'
                            var dependentIds = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id)
                                .Select(u => u.Id)
                                .ToListAsync();
                            transactionsQuery = transactionsQuery
                                .Where(t => t.Account != null && 
                                          (t.Account.UserId == localUser.Id || 
                                           dependentIds.Contains(t.Account.UserId ?? 0)));
                            break;
                        
                        case "Caregiver":
                            // Caregivers see transactions of assigned patients
                            var patientIds = await _context.Users
                                .Where(u => u.Role == "Dependent" && u.BlockedBy == localUser.Id) // Using BlockedBy temporarily as relationship
                                .Select(u => u.Id)
                                .ToListAsync();
                            transactionsQuery = transactionsQuery
                                .Where(t => t.Account != null && patientIds.Contains(t.Account.UserId ?? 0));
                            break;
                        
                        default:
                            // Admin sees all transactions
                            break;
                    }

                    // Apply type filter if provided
                    if (!string.IsNullOrEmpty(type))
                    {
                        transactionsQuery = transactionsQuery.Where(t => t.Type == type);
                    }

                    // Get paginated results
                    var paginatedTransactions = await transactionsQuery
                        .OrderByDescending(t => t.Date)
                        .Select(t => new
                        {
                            t.Id,
                            t.AccountId,
                            t.Amount,
                            t.Date,
                            t.Type,
                            t.Description,
                            t.Status,
                            AccountNumber = t.Account != null ? t.Account.AccountNumber : null,
                            UserId = t.Account != null ? t.Account.UserId : null
                        })
                        .Take(limit)
                        .ToListAsync();

                    // Get total count for pagination
                    var totalCount = await transactionsQuery.CountAsync();

                    // Return with pagination info
                    return Ok(new
                    {
                        data = paginatedTransactions,
                        total = totalCount,
                        limit = limit,
                        hasMore = totalCount > limit
                    });
                }

                // Try external API if user not found locally
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/portal/me/transactions");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<dynamic>(responseJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        return Ok(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                }

                // Return empty result if no transactions found
                return Ok(new
                {
                    data = new List<object>(),
                    total = 0,
                    limit = limit,
                    hasMore = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user transactions: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "New password is required" });
                }

                if (request.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Password must be at least 6 characters long" });
                }

                // Extract user email from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "No valid token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userEmail = _jwtService.GetEmailFromToken(token);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Try external API for password reset
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var jsonContent = JsonSerializer.Serialize(request);
                    var content = new StringContent(jsonContent, Encoding.UTF8);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await _httpClient.PostAsync($"{API_BASE_URL}/api/portal/reset-password", content);
                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(new { message = "Password reset successfully" });
                    }
                    else
                    {
                        var errorJson = await response.Content.ReadAsStringAsync();
                        return BadRequest(new { message = "Failed to reset password", error = errorJson });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                    return StatusCode(500, new { message = "Failed to reset password", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting password: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for password reset
    /// </summary>
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
