using CMS.Web.Models;
using CMS.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CMS.Web.Services
{
    public interface IAuthService
    {
        Task<AdminLoginResponse> AuthenticateAdminAsync(AdminLoginRequest request);
        Task<AdminLoginResponse> AuthenticateAdminLocalAsync(AdminLoginRequest request);
        Task<User?> GetUserByEmailAsync(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private readonly CmsDbContext _context;
        private readonly IJwtService _jwtService;
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com";

        public AuthService(HttpClient httpClient, ILogger<AuthService> logger, CmsDbContext context, IJwtService jwtService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AdminLoginResponse> AuthenticateAdminAsync(AdminLoginRequest request)
        {
            var endpoints = new[]
            {
                "/auth/admin-login",
                "/auth/login",
                "/api/auth/admin-login",
                "/api/auth/login",
                "/admin/login",
                "/admin/auth/login"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    _logger.LogInformation($"Attempting admin login for email: {request.Email} at endpoint: {endpoint}");
                    
                    var response = await _httpClient.PostAsync($"{API_BASE_URL}{endpoint}", content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"Endpoint {endpoint} - Status: {response.StatusCode}");
                    _logger.LogInformation($"Endpoint {endpoint} - Response: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var loginResponse = JsonSerializer.Deserialize<AdminLoginResponse>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            if (loginResponse != null)
                            {
                                _logger.LogInformation($"Successful login at endpoint: {endpoint}");
                                _logger.LogInformation($"Login response - Success: {loginResponse.Success}, Token: {(string.IsNullOrEmpty(loginResponse.Token) ? "None" : "Present")}");
                                return loginResponse;
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, $"Failed to deserialize login response from {endpoint}");
                            _logger.LogInformation($"Raw response content: {responseContent}");
                            
                            // Try to create a response from raw JSON if it contains an access token
                            if (responseContent.Contains("accessToken") || responseContent.Contains("access_token") || responseContent.Contains("token"))
                            {
                                try
                                {
                                    var rawResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                                    var token = "";
                                    
                                    if (rawResponse.TryGetProperty("accessToken", out var accessTokenElement))
                                        token = accessTokenElement.GetString() ?? "";
                                    else if (rawResponse.TryGetProperty("access_token", out var access_tokenElement))
                                        token = access_tokenElement.GetString() ?? "";
                                    else if (rawResponse.TryGetProperty("token", out var tokenElement))
                                        token = tokenElement.GetString() ?? "";

                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        return new AdminLoginResponse
                                        {
                                            AccessToken = token,
                                            User = new AdminUser
                                            {
                                                Id = 1,
                                                Email = request.Email,
                                                FirstName = "Admin",
                                                Surname = "User",
                                                Role = "admin"
                                            }
                                        };
                                    }
                                }
                                catch (Exception parseEx)
                                {
                                    _logger.LogError(parseEx, "Failed to parse raw token response");
                                }
                            }
                        }
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        // If it's not 404, this might be the right endpoint but wrong credentials
                        // Return the response so we can see the error
                        return new AdminLoginResponse 
                        { 
                            Message = $"Login failed at {endpoint}: {responseContent}"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error testing endpoint {endpoint}");
                }
            }

            return new AdminLoginResponse 
            { 
                Message = "No valid login endpoint found"
            };
        }

        /// <summary>
        /// Authenticate admin user against local database and generate JWT token
        /// </summary>
        public async Task<AdminLoginResponse> AuthenticateAdminLocalAsync(AdminLoginRequest request)
        {
            try
            {
                // Find user in local database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == "admin");

                if (user == null)
                {
                    return new AdminLoginResponse
                    {
                        Message = "Invalid email or password"
                    };
                }

                // Check if user is blocked
                if (user.IsBlocked)
                {
                    return new AdminLoginResponse
                    {
                        Message = "Account is blocked or suspended"
                    };
                }

                // In a real implementation, you would verify the password hash
                // For now, we'll assume authentication is successful
                // TODO: Implement proper password hashing and verification

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                return new AdminLoginResponse
                {
                    AccessToken = token,
                    Jwt = token,
                    User = new AdminUser
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        MiddleName = user.MiddleName,
                        Surname = user.Surname,
                        Email = user.Email,
                        Role = user.Role
                    },
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during local admin authentication");
                return new AdminLoginResponse
                {
                    Message = "Authentication failed"
                };
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            try
            {
                // Try local database first
                var localUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (localUser != null)
                    return localUser;

                // If not found locally, try external API
                _httpClient.DefaultRequestHeaders.Clear();

                var response = await _httpClient.GetAsync($"{API_BASE_URL}/api/users/by-email/{email}");
                if (response.IsSuccessStatusCode)
                {
                    var userJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<User>(userJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
            }

            return null;
        }
    }
}
