using CMS.Web.Models;
using System.Text;
using System.Text.Json;

namespace CMS.Web.Services
{
    public interface IAuthService
    {
        Task<AdminLoginResponse> AuthenticateAdminAsync(AdminLoginRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com";

        public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
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
    }
}
