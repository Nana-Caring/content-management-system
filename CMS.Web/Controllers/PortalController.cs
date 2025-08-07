/*
 * This controller was created as a fallback when the external backend had schema issues.
 * Since the backend issues are now resolved, this controller is no longer needed.
 * The portal now works with the external backend at https://nanacaring-backend.onrender.com
 *
 * The controller code has been commented out to prevent compilation errors since
 * we're no longer using it, but it's kept for reference in case we need to
 * reimplement any of its functionality locally in the future.
 */

/* 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Web.Data;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace CMS.Web.Controllers
{
    [ApiController]
    [Route("api/portal")]
    public class PortalController : ControllerBase
    {
        private readonly CmsDbContext _context;
        private readonly IAuthService _authService;
        private readonly JwtService _jwtService;
        private readonly ILogger<PortalController> _logger;
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com";

        public PortalController(
            CmsDbContext context, 
            IAuthService authService, 
            JwtService jwtService,
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
        /// Portal login endpoint
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> PortalLogin([FromBody] AdminLoginRequest loginRequest)
        {
            try
            {
                // Find user in local database with the provided email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginRequest.Email.ToLower());

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check if user is blocked
                if (user.IsBlocked)
                {
                    return Unauthorized(new { message = "Account is blocked or suspended" });
                }

                // For portal access, we'll accept the stored credentials
                // In a real system, you'd verify the password hash
                // Generate JWT token for the user
                var token = _jwtService.GenerateToken(user);

                return Ok(new 
                { 
                    token = token,
                    accessToken = token,
                    user = new 
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        middleName = user.MiddleName,
                        surname = user.Surname,
                        role = user.Role
                    },
                    message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during portal login for email: {Email}", loginRequest.Email);
                return StatusCode(500, new { message = "Login failed", error = ex.Message });
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
                    return Ok(localUser);
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
                    // Update local user
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
                    // Get accounts from local database (if any exist)
                    var localAccounts = await _context.Set<Account>()
                        .Where(a => a.UserId == localUser.Id)
                        .ToListAsync();

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
        public async Task<IActionResult> GetUserTransactions()
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

                // Try external API
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/portal/me/transactions");
                    if (response.IsSuccessStatusCode)
                    {
                        var transactionsJson = await response.Content.ReadAsStringAsync();
                        var transactions = JsonSerializer.Deserialize<List<Transaction>>(transactionsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        return Ok(transactions ?? new List<Transaction>());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"External API call failed: {ex.Message}");
                }

                // Return empty list if no transactions found
                return Ok(new List<Transaction>());
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
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

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
*/
