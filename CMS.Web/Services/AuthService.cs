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

                    if (loginResponse != null && loginResponse.Success)
                    {
                        _logger.LogInformation($"Admin login successful for email: {request.Email}");
                        return loginResponse;
                    }
                    else
                    {
                        _logger.LogWarning($"Admin login failed - invalid response format for email: {request.Email}");
                        return new AdminLoginResponse
                        {
                            Message = "Invalid response format from server"
                        };
                    }
                }
                else
                {
                    _logger.LogWarning($"Admin login failed with status {response.StatusCode} for email: {request.Email}");
                    
                    // Try to parse error response for a meaningful message
                    try
                    {
                        var errorData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        var errorMessage = "Authentication failed";
                        
                        if (errorData.TryGetProperty("message", out var msgElement))
                        {
                            errorMessage = msgElement.GetString() ?? errorMessage;
                        }
                        else if (errorData.TryGetProperty("error", out var errElement))
                        {
                            errorMessage = errElement.GetString() ?? errorMessage;
                        }
                        
                        return new AdminLoginResponse
                        {
                            Message = errorMessage
                        };
                    }
                    catch
                    {
                        return new AdminLoginResponse
                        {
                            Message = $"Authentication failed: {response.StatusCode}"
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed during admin authentication");
                return new AdminLoginResponse
                {
                    Message = "Network error occurred. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during admin authentication");
                return new AdminLoginResponse
                {
                    Message = "An unexpected error occurred. Please try again."
                };
            }
        }
    }
}
