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
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com/api";

        public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AdminLoginResponse> AuthenticateAdminAsync(AdminLoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation($"Attempting admin login for email: {request.Email}");
                
                var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/admin-login", content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<AdminLoginResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return loginResponse ?? new AdminLoginResponse
                    {
                        Success = false,
                        Message = "Invalid response format"
                    };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<AdminLoginResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        
                        return errorResponse ?? new AdminLoginResponse
                        {
                            Success = false,
                            Message = $"Authentication failed with status: {response.StatusCode}"
                        };
                    }
                    catch
                    {
                        return new AdminLoginResponse
                        {
                            Success = false,
                            Message = $"Authentication failed: {response.StatusCode} - {responseContent}"
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed during admin authentication");
                return new AdminLoginResponse
                {
                    Success = false,
                    Message = "Network error occurred. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during admin authentication");
                return new AdminLoginResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again."
                };
            }
        }
    }
}
