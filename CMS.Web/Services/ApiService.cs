using CMS.Web.Models;
using System.Text;
using System.Text.Json;

namespace CMS.Web.Services
{
    public interface IApiService
    {
        // Users
        Task<List<User>> GetUsersAsync();
        Task<List<User>> GetUsersAsync(string? search = null, string? roleFilter = null, string? relationFilter = null, DateTime? createdFrom = null, DateTime? createdTo = null, string? sortBy = null, string? sortDirection = null);
        Task<(List<User> users, int total)> GetUsersWithCountAsync(string? search = null, string? roleFilter = null, string? relationFilter = null, DateTime? createdFrom = null, DateTime? createdTo = null, string? sortBy = null, string? sortDirection = null);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> DeleteUserAsync(int id);
        Task<ApiResponse<User>> BlockUserAsync(int userId, string reason);
        Task<ApiResponse<User>> UnblockUserAsync(int userId);
        Task<ApiResponse<User>> SuspendUserAsync(int userId, string reason);
        Task<ApiResponse<User>> SuspendUserAsync(int userId, string reason, int suspensionDays);
        Task<ApiResponse<User>> UnsuspendUserAsync(int userId);
        Task<ApiResponse<object>> DeleteUserPermanentlyAsync(int userId, bool confirmDelete, bool deleteData = true);
        Task<UserStatsResponse> GetUserStatsAsync();
        Task<ApiResponse<object>> CheckExpiredSuspensionsAsync();

        // Accounts
        Task<List<Account>> GetAccountsAsync();
        Task<Account?> GetAccountByIdAsync(int id);
        Task<bool> DeleteAccountAsync(int id);

        // Transactions
        Task<List<Transaction>> GetTransactionsAsync();
        Task<Transaction?> GetTransactionByIdAsync(int id);
        Task<bool> DeleteTransactionAsync(int id);

        // Products (legacy minimal view)
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);

        // Admin Products (full CRUD)
        Task<(List<AdminProduct> products, PaginationInfo? pagination)> GetAdminProductsAsync(
            string? category = null, string? brand = null, bool? inStock = null, bool? isActive = null,
            string? search = null, int? page = null, int? limit = null, string? sortBy = null, string? sortOrder = null);
        Task<AdminProduct?> GetAdminProductByIdOrSkuAsync(string idOrSku);
        Task<ApiResponse<AdminProduct>> CreateAdminProductAsync(AdminProductCreateRequest request);
        Task<ApiResponse<AdminProduct>> UpdateAdminProductAsync(int id, AdminProductUpdateRequest request);
        Task<ApiResponse<object>> DeleteAdminProductAsync(int id);

        // KYC Requests
        Task<List<KycRequest>> GetKycRequestsAsync();
        Task<KycRequest?> GetKycRequestByIdAsync(int id);

        // Dashboard Stats
        Task<DashboardStats> GetDashboardStatsAsync();
        
        // Generic HTTP methods
        Task<T?> PostAsync<T>(string endpoint, object data);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly IAppStateManager _stateManager;
        private const string API_BASE_URL = "https://nanacaring-backend.onrender.com";

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IAppStateManager stateManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _stateManager = stateManager;
        }

        private async Task SetAuthenticationHeaders()
        {
            var state = await _stateManager.GetAppStateAsync();
            _logger.LogInformation($"App State Retrieved: {(state != null ? "Yes" : "No")}");
            _logger.LogInformation($"UserInfo Available: {(state?.UserInfo != null ? "Yes" : "No")}");
            _logger.LogInformation($"Token Available: {(state?.UserInfo?.Token != null ? "Yes" : "No")}");
            
            if (state?.UserInfo?.Token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", state.UserInfo.Token);
                _logger.LogInformation($"Setting Bearer token for API request (token length: {state.UserInfo.Token.Length})");
                _logger.LogInformation($"Token preview: {state.UserInfo.Token.Substring(0, Math.Min(20, state.UserInfo.Token.Length))}...");
            }
            else
            {
                _logger.LogWarning("No authentication token found in user state");
                if (state?.UserInfo != null)
                {
                    _logger.LogInformation($"UserInfo exists - IsAuthenticated: {state.UserInfo.IsAuthenticated}, Username: {state.UserInfo.Username}");
                }
            }
        }

        private async Task<string> GetRawResponseAsync(string endpoint)
        {
            try
            {
                await SetAuthenticationHeaders();
                
                _logger.LogInformation($"Making raw GET request to: {API_BASE_URL}{endpoint}");
                
                var response = await _httpClient.GetAsync($"{API_BASE_URL}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Raw API Response Status: {response.StatusCode}");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetRawResponseAsync");
                return string.Empty;
            }
        }

        private async Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                await SetAuthenticationHeaders();
                
                _logger.LogInformation($"Making GET request to: {API_BASE_URL}{endpoint}");
                
                var response = await _httpClient.GetAsync($"{API_BASE_URL}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _logger.LogInformation($"Successfully deserialized response to type {typeof(T).Name}");
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning($"Backend service unavailable (503) - this may be a Render.com cold start. Retrying in 10 seconds...");
                    await Task.Delay(10000); // Wait 10 seconds for service to wake up
                    
                    // Retry once with longer timeout
                    var retryResponse = await _httpClient.GetAsync($"{API_BASE_URL}{endpoint}");
                    var retryContent = await retryResponse.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"Retry API Response Status: {retryResponse.StatusCode}");
                    _logger.LogInformation($"Retry API Response Content: {retryContent}");
                    
                    if (retryResponse.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<T>(retryContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation($"Successfully deserialized retry response to type {typeof(T).Name}");
                        return result;
                    }
                    else if (retryResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        _logger.LogWarning($"Backend still unavailable after retry. Trying one more time with 15 second delay...");
                        await Task.Delay(15000); // Wait 15 seconds for service to fully wake up
                        
                        // Final retry
                        var finalResponse = await _httpClient.GetAsync($"{API_BASE_URL}{endpoint}");
                        var finalContent = await finalResponse.Content.ReadAsStringAsync();
                        
                        _logger.LogInformation($"Final Retry API Response Status: {finalResponse.StatusCode}");
                        
                        if (finalResponse.IsSuccessStatusCode)
                        {
                            var result = JsonSerializer.Deserialize<T>(finalContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            return result;
                        }
                        else
                        {
                            _logger.LogError($"Final retry also failed with status {finalResponse.StatusCode}: {finalContent}");
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogError($"Retry failed with status {retryResponse.StatusCode}: {retryContent}");
                        return null;
                    }
                }
                else
                {
                    _logger.LogError($"API request failed with status {response.StatusCode}: {content}");
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request exception for {endpoint} - this might be a network connectivity issue or backend downtime");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Request timeout for {endpoint} - backend might be slow to respond or experiencing issues");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making GET request to {endpoint}");
                return null;
            }
        }

        private async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                await SetAuthenticationHeaders();
                
                _logger.LogInformation($"Making DELETE request to: {API_BASE_URL}{endpoint}");
                
                var response = await _httpClient.DeleteAsync($"{API_BASE_URL}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"DELETE request failed with status {response.StatusCode}: {content}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making DELETE request to {endpoint}");
                return false;
            }
        }

        // Users
        public async Task<List<User>> GetUsersAsync()
        {
            _logger.LogInformation("GetUsersAsync called - starting user retrieval");
            var response = await GetAsync<UserListResponse>("/admin/users");
            _logger.LogInformation($"GetUsersAsync completed - returned {(response?.Data?.Users?.Count ?? 0)} users, Total: {response?.Data?.Total ?? 0}");
            
            if (response?.Data?.Users != null && response.Data.Users.Any())
            {
                _logger.LogInformation($"Sample user data: ID={response.Data.Users[0].Id}, Name={response.Data.Users[0].FullName}, Email={response.Data.Users[0].Email}, Role={response.Data.Users[0].Role}");
            }
            
            return response?.Data?.Users ?? new List<User>();
        }

        public async Task<List<User>> GetUsersAsync(string? search = null, string? roleFilter = null, string? relationFilter = null, DateTime? createdFrom = null, DateTime? createdTo = null, string? sortBy = null, string? sortDirection = null)
        {
            try
            {
                _logger.LogInformation($"GetUsersAsync (filtered) called - Search: {search}, Role: {roleFilter}, Relation: {relationFilter}");
                
                // Build query parameters for filtering (no pagination)
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                
                if (!string.IsNullOrEmpty(roleFilter))
                    queryParams.Add($"role={Uri.EscapeDataString(roleFilter)}");
                
                if (!string.IsNullOrEmpty(relationFilter))
                    queryParams.Add($"relation={Uri.EscapeDataString(relationFilter)}");
                
                if (createdFrom.HasValue)
                    queryParams.Add($"createdFrom={createdFrom.Value:yyyy-MM-dd}");
                
                if (createdTo.HasValue)
                    queryParams.Add($"createdTo={createdTo.Value:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(sortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
                else
                    queryParams.Add("sortBy=id"); // Default to id
                
                if (!string.IsNullOrEmpty(sortDirection))
                    queryParams.Add($"sortDirection={Uri.EscapeDataString(sortDirection)}");
                
                var endpoint = "/admin/users";
                if (queryParams.Any())
                {
                    var queryString = string.Join("&", queryParams);
                    endpoint = $"/admin/users?{queryString}";
                }
                
                _logger.LogInformation($"Making filtered request to: {endpoint}");
                
                var response = await GetAsync<UserListResponse>(endpoint);
                
                if (response?.Success == true && response.Data?.Users != null)
                {
                    _logger.LogInformation($"GetUsersAsync (filtered) completed - returned {response.Data.Users.Count} users");
                    return response.Data.Users;
                }
                else
                {
                    _logger.LogWarning("GetUsersAsync (filtered) - API response was not successful or data was null");
                    return new List<User>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersAsync (filtered)");
                return new List<User>();
            }
        }

        public async Task<(List<User> users, int total)> GetUsersWithCountAsync(string? search = null, string? roleFilter = null, string? relationFilter = null, DateTime? createdFrom = null, DateTime? createdTo = null, string? sortBy = null, string? sortDirection = null)
        {
            try
            {
                _logger.LogInformation($"GetUsersWithCountAsync called - Search: {search}, Role: {roleFilter}, Relation: {relationFilter}");
                
                // Build query parameters for filtering (no pagination)
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                
                if (!string.IsNullOrEmpty(roleFilter))
                    queryParams.Add($"role={Uri.EscapeDataString(roleFilter)}");
                
                if (!string.IsNullOrEmpty(relationFilter))
                    queryParams.Add($"relation={Uri.EscapeDataString(relationFilter)}");
                
                if (createdFrom.HasValue)
                    queryParams.Add($"createdFrom={createdFrom.Value:yyyy-MM-dd}");
                
                if (createdTo.HasValue)
                    queryParams.Add($"createdTo={createdTo.Value:yyyy-MM-dd}");
                
                if (!string.IsNullOrEmpty(sortBy))
                    queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
                else
                    queryParams.Add("sortBy=id"); // Default to id
                
                if (!string.IsNullOrEmpty(sortDirection))
                    queryParams.Add($"sortDirection={Uri.EscapeDataString(sortDirection)}");
                
                var endpoint = "/admin/users";
                if (queryParams.Any())
                {
                    var queryString = string.Join("&", queryParams);
                    endpoint = $"/admin/users?{queryString}";
                }
                
                _logger.LogInformation($"Making filtered request with count to: {endpoint}");
                
                var response = await GetAsync<UserListResponse>(endpoint);
                
                if (response?.Success == true && response.Data?.Users != null)
                {
                    var users = response.Data.Users;
                    var total = response.Data.Total;
                    _logger.LogInformation($"GetUsersWithCountAsync completed - returned {users.Count} users, Total: {total}");
                    return (users, total);
                }
                else
                {
                    _logger.LogWarning("GetUsersWithCountAsync - API response was not successful or data was null");
                    return (new List<User>(), 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersWithCountAsync");
                return (new List<User>(), 0);
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var response = await GetAsync<UserDetailResponse>($"/admin/users/{id}");
            return response?.Data?.User;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await DeleteAsync($"/admin/users/{id}");
        }

        public async Task<ApiResponse<User>> BlockUserAsync(int userId, string reason)
        {
            try
            {
                var requestData = new { reason = reason };
                var response = await PutAsync($"/admin/users/{userId}/block", requestData);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<BlockUserResponse>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return new ApiResponse<User> 
                        { 
                            Success = true, 
                            Message = result?.Message ?? "User blocked successfully",
                            Data = result?.User 
                        };
                    }
                    return new ApiResponse<User> { Success = true, Message = "User blocked successfully" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<User> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to block user: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error blocking user {userId}");
                return new ApiResponse<User> { Success = false, Message = "An error occurred while blocking the user" };
            }
        }

        public async Task<ApiResponse<User>> UnblockUserAsync(int userId)
        {
            try
            {
                var response = await PutAsync($"/admin/users/{userId}/unblock", new { });
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<BlockUserResponse>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return new ApiResponse<User> 
                        { 
                            Success = true, 
                            Message = result?.Message ?? "User unblocked successfully",
                            Data = result?.User 
                        };
                    }
                    return new ApiResponse<User> { Success = true, Message = "User unblocked successfully" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<User> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to unblock user: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unblocking user {userId}");
                return new ApiResponse<User> { Success = false, Message = "An error occurred while unblocking the user" };
            }
        }

        // Overload for backward compatibility - defaults to 7 days suspension
        public async Task<ApiResponse<User>> SuspendUserAsync(int userId, string reason)
        {
            return await SuspendUserAsync(userId, reason, 7); // Default to 7 days
        }

        public async Task<ApiResponse<User>> SuspendUserAsync(int userId, string reason, int suspensionDays)
        {
            try
            {
                var requestData = new { reason = reason, suspensionDays = suspensionDays };
                var response = await PutAsync($"/admin/users/{userId}/suspend", requestData);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<BlockUserResponse>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return new ApiResponse<User> 
                        { 
                            Success = true, 
                            Message = result?.Message ?? "User suspended successfully",
                            Data = result?.User 
                        };
                    }
                    return new ApiResponse<User> { Success = true, Message = "User suspended successfully" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<User> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to suspend user: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error suspending user {userId}");
                return new ApiResponse<User> { Success = false, Message = "An error occurred while suspending the user" };
            }
        }

        public async Task<ApiResponse<User>> UnsuspendUserAsync(int userId)
        {
            try
            {
                var response = await PutAsync($"/admin/users/{userId}/unsuspend", new { });
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<BlockUserResponse>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return new ApiResponse<User> 
                        { 
                            Success = true, 
                            Message = result?.Message ?? "User unsuspended successfully",
                            Data = result?.User 
                        };
                    }
                    return new ApiResponse<User> { Success = true, Message = "User unsuspended successfully" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<User> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to unsuspend user: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unsuspending user {userId}");
                return new ApiResponse<User> { Success = false, Message = "An error occurred while unsuspending the user" };
            }
        }

        public async Task<ApiResponse<object>> DeleteUserPermanentlyAsync(int userId, bool confirmDelete, bool deleteData = true)
        {
            try
            {
                var requestData = new { confirmDelete = confirmDelete, deleteData = deleteData };
                var response = await DeleteWithBodyAsync($"/admin/users/{userId}", requestData);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return result ?? new ApiResponse<object> { Success = true, Message = "User deleted successfully" };
                    }
                    return new ApiResponse<object> { Success = true, Message = "User deleted successfully" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to delete user: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {userId}");
                return new ApiResponse<object> { Success = false, Message = "An error occurred while deleting the user" };
            }
        }

        public async Task<UserStatsResponse> GetUserStatsAsync()
        {
            try
            {
                await SetAuthenticationHeaders();
                
                _logger.LogInformation($"Making GET request to: {API_BASE_URL}/admin/users/stats");
                
                var response = await _httpClient.GetAsync($"{API_BASE_URL}/admin/users/stats");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User stats API Response: {content}");
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<UserStatsResponse>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return result ?? new UserStatsResponse { Success = false };
                    }
                }
                
                _logger.LogWarning($"Failed to fetch user stats: {response.StatusCode}");
                return new UserStatsResponse { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return new UserStatsResponse { Success = false };
            }
        }

        public async Task<ApiResponse<object>> CheckExpiredSuspensionsAsync()
        {
            try
            {
                var response = await PostAsync("/admin/users/check-expired-suspensions", new { });
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        return result ?? new ApiResponse<object> { Success = true, Message = "Expired suspensions checked" };
                    }
                    return new ApiResponse<object> { Success = true, Message = "Expired suspensions checked" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Failed to check expired suspensions: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired suspensions");
                return new ApiResponse<object> { Success = false, Message = "An error occurred while checking expired suspensions" };
            }
        }

        private async Task<HttpResponseMessage> DeleteWithBodyAsync(string endpoint, object data)
        {
            try
            {
                await SetAuthenticationHeaders();
                
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{API_BASE_URL}{endpoint}")
                {
                    Content = content
                };
                
                _logger.LogInformation($"Making DELETE request with body to: {API_BASE_URL}{endpoint}");
                
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {responseContent}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making DELETE request to {endpoint}");
                throw;
            }
        }

        private async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            try
            {
                await SetAuthenticationHeaders();
                
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation($"Making POST request to: {API_BASE_URL}{endpoint}");
                _logger.LogInformation($"POST Request JSON: {json}");
                
                var response = await _httpClient.PostAsync($"{API_BASE_URL}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {responseContent}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making POST request to {endpoint}");
                throw;
            }
        }

        private async Task<HttpResponseMessage> PutAsync(string endpoint, object data)
        {
            try
            {
                await SetAuthenticationHeaders();
                
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation($"Making PUT request to: {API_BASE_URL}{endpoint}");
                _logger.LogInformation($"PUT Request JSON: {json}");
                
                var response = await _httpClient.PutAsync($"{API_BASE_URL}{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {responseContent}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making PUT request to {endpoint}");
                throw;
            }
        }

        // Accounts
        public async Task<List<Account>> GetAccountsAsync()
        {
            _logger.LogInformation("GetAccountsAsync called - starting account retrieval");
            
            try
            {
                // First, let's get the raw response to see what we're receiving
                var rawResponse = await GetRawResponseAsync("/admin/accounts");
                _logger.LogInformation($"Raw accounts response: {rawResponse}");
                
                var accounts = await GetAsync<List<Account>>("/admin/accounts");
                _logger.LogInformation($"GetAccountsAsync completed - returned {(accounts?.Count ?? 0)} accounts");
                
                if (accounts == null)
                {
                    _logger.LogWarning("GetAccountsAsync: Received null response from API");
                    return new List<Account>();
                }
                
                if (!accounts.Any())
                {
                    _logger.LogWarning("GetAccountsAsync: Received empty list from API");
                }
                else
                {
                    _logger.LogInformation($"Sample account data: ID={accounts[0].Id}, Number={accounts[0].AccountNumber}, Name={accounts[0].AccountName}, Balance={accounts[0].Balance}");
                    
                    // Log all account details for debugging
                    for (int i = 0; i < Math.Min(accounts.Count, 3); i++)
                    {
                        var acc = accounts[i];
                        _logger.LogInformation($"Account {i}: ID={acc.Id}, Number={acc.AccountNumber}, Name={acc.AccountName}, Type={acc.AccountType}, Balance={acc.Balance}, UserId={acc.UserId}, Created={acc.CreatedAt}");
                    }
                }
                
                return accounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAccountsAsync");
                return new List<Account>();
            }
        }

        public async Task<Account?> GetAccountByIdAsync(int id)
        {
            return await GetAsync<Account>($"/admin/accounts/{id}");
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            return await DeleteAsync($"/admin/accounts/{id}");
        }

        // Transactions
        public async Task<List<Transaction>> GetTransactionsAsync()
        {
            var transactions = await GetAsync<List<Transaction>>("/admin/transactions");
            return transactions ?? new List<Transaction>();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int id)
        {
            return await GetAsync<Transaction>($"/admin/transactions/{id}");
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            return await DeleteAsync($"/admin/transactions/{id}");
        }

        // Products (legacy minimal view)
        public async Task<List<Product>> GetProductsAsync()
        {
            // Pull all admin products across pages, then map to minimal view model used by the page
            var allAdmin = new List<AdminProduct>();
            int page = 1;
            const int limit = 100; // reasonable page size to reduce requests
            int safety = 0; // avoid runaway loops

            while (true)
            {
                var (products, pagination) = await GetAdminProductsAsync(
                    category: null, brand: null, inStock: null, isActive: null,
                    search: null, page: page, limit: limit, sortBy: null, sortOrder: null);

                if (products != null && products.Count > 0)
                {
                    allAdmin.AddRange(products);
                }

                // Decide when to stop
                int totalPages = 0;
                if (pagination != null)
                {
                    totalPages = pagination.TotalPages != 0 ? pagination.TotalPages : pagination.Pages;
                }

                if (totalPages > 0)
                {
                    if (page >= totalPages) break;
                    page++;
                }
                else
                {
                    // Fallback when pagination info isn't present: stop if less than limit returned
                    if (products == null || products.Count < limit) break;
                    page++;
                }

                if (++safety > 100) break; // hard cap to prevent infinite loops in case of API anomalies
            }

            var mapped = allAdmin.Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                Description = p.Description ?? string.Empty,
                ApiLink = string.Empty,
                CreatedAt = p.CreatedAt ?? DateTime.MinValue
            }).ToList();

            return mapped;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var wrapper = await GetAsync<AdminProductResponse>($"/admin/products/{id}");
            var p = wrapper?.Data;
            if (p == null) return null;
            return new Product
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                Description = p.Description ?? string.Empty,
                ApiLink = string.Empty,
                CreatedAt = p.CreatedAt ?? DateTime.MinValue
            };
        }

        // Admin Products (full CRUD)
        public async Task<(List<AdminProduct> products, PaginationInfo? pagination)> GetAdminProductsAsync(
            string? category = null, string? brand = null, bool? inStock = null, bool? isActive = null,
            string? search = null, int? page = null, int? limit = null, string? sortBy = null, string? sortOrder = null)
        {
            var qs = new List<string>();
            if (!string.IsNullOrEmpty(category)) qs.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(brand)) qs.Add($"brand={Uri.EscapeDataString(brand)}");
            if (inStock.HasValue) qs.Add($"inStock={(inStock.Value ? "true" : "false")}");
            if (isActive.HasValue) qs.Add($"isActive={(isActive.Value ? "true" : "false")}");
            if (!string.IsNullOrEmpty(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
            if (page.HasValue) qs.Add($"page={page.Value}");
            if (limit.HasValue) qs.Add($"limit={limit.Value}");
            if (!string.IsNullOrEmpty(sortBy)) qs.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
            if (!string.IsNullOrEmpty(sortOrder)) qs.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
            var endpoint = "/api/products" + (qs.Count > 0 ? "?" + string.Join("&", qs) : string.Empty);

            var wrapper = await GetAsync<AdminProductListResponse>(endpoint);
            return (wrapper?.Data ?? new List<AdminProduct>(), wrapper?.Pagination);
        }

        public async Task<AdminProduct?> GetAdminProductByIdOrSkuAsync(string idOrSku)
        {
            var wrapper = await GetAsync<AdminProductResponse>($"/admin/products/{idOrSku}");
            return wrapper?.Data;
        }

        public async Task<ApiResponse<AdminProduct>> CreateAdminProductAsync(AdminProductCreateRequest request)
        {
            try
            {
                var response = await PostAsync("/admin/products", request);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
                {
                    var result = JsonSerializer.Deserialize<AdminProductResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new ApiResponse<AdminProduct> { Success = result?.Success ?? false, Message = result?.Message ?? string.Empty, Data = result?.Data };
                }
                return new ApiResponse<AdminProduct> { Success = false, Message = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAdminProductAsync error");
                return new ApiResponse<AdminProduct> { Success = false, Message = "Failed to create product" };
            }
        }

        public async Task<ApiResponse<AdminProduct>> UpdateAdminProductAsync(int id, AdminProductUpdateRequest request)
        {
            try
            {
                var response = await PutAsync($"/admin/products/{id}", request);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
                {
                    var result = JsonSerializer.Deserialize<AdminProductResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new ApiResponse<AdminProduct> { Success = result?.Success ?? false, Message = result?.Message ?? string.Empty, Data = result?.Data };
                }
                return new ApiResponse<AdminProduct> { Success = false, Message = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAdminProductAsync error");
                return new ApiResponse<AdminProduct> { Success = false, Message = "Failed to update product" };
            }
        }

        public async Task<ApiResponse<object>> DeleteAdminProductAsync(int id)
        {
            try
            {
                var ok = await DeleteAsync($"/admin/products/{id}");
                return new ApiResponse<object> { Success = ok, Message = ok ? "Product deleted successfully" : "Failed to delete product" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAdminProductAsync error");
                return new ApiResponse<object> { Success = false, Message = "Failed to delete product" };
            }
        }

        // KYC Requests
        public async Task<List<KycRequest>> GetKycRequestsAsync()
        {
            var kycRequests = await GetAsync<List<KycRequest>>("/admin/kyc");
            return kycRequests ?? new List<KycRequest>();
        }

        public async Task<KycRequest?> GetKycRequestByIdAsync(int id)
        {
            return await GetAsync<KycRequest>($"/admin/kyc/{id}");
        }

        // Dashboard Stats
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                _logger.LogInformation("GetDashboardStatsAsync called - starting stats retrieval");
                // The Express API returns { users: count, accounts: count, transactions: count }
                var apiStats = await GetAsync<ApiStatsResponse>("/admin/stats");
                if (apiStats != null)
                {
                    _logger.LogInformation($"Stats retrieved - Users: {apiStats.Users}, Accounts: {apiStats.Accounts}, Transactions: {apiStats.Transactions}");
                    return new DashboardStats
                    {
                        TotalUsers = apiStats.Users,
                        TotalAccounts = apiStats.Accounts,
                        TotalTransactions = apiStats.Transactions,
                        PendingKycRequests = 0, // Default or calculate if available
                        TotalBalance = 0, // Default or calculate if available
                        TodayTransactionAmount = 0, // Default or calculate if available
                        NewUsersToday = 0, // Default or calculate if available
                        ActiveUsers = 0, // Default or calculate if available
                        TotalProducts = 0 // Default or calculate if available
                    };
                }
                else
                {
                    _logger.LogWarning("GetDashboardStatsAsync - apiStats is null, returning default stats");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats from API");
            }
            
            _logger.LogInformation("GetDashboardStatsAsync - returning default empty stats");
            return new DashboardStats();
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var response = await PostAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (typeof(T) == typeof(object))
                {
                    return (T)(object)new { success = true };
                }
                
                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in generic POST request to {endpoint}");
                return default(T);
            }
        }
    }

    // API Response Models
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class BlockUserResponse
    {
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }

    public class ApiStatsResponse
    {
        public int Users { get; set; }
        public int Accounts { get; set; }
        public int Transactions { get; set; }
    }

    // Dashboard Stats Model
    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingKycRequests { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TodayTransactionAmount { get; set; }
        public int NewUsersToday { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalProducts { get; set; } // Added for dashboard
    }

    // External API Response Models
    public class UserListResponse
    {
        public bool Success { get; set; }
        public UserListData? Data { get; set; }
    }

    public class UserListData
    {
        public List<User> Users { get; set; } = new List<User>();
        public int Total { get; set; }
        public PaginationInfo? Pagination { get; set; } // Keep for backward compatibility
    }

    // PaginationInfo moved to CMS.Web.Models.PaginationInfo

    public class UserStatsResponse
    {
        public bool Success { get; set; }
        public UserStatsData? Data { get; set; }
    }

    public class UserStatsData
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Blocked { get; set; }
        public int Suspended { get; set; }
    }

    public class UserDetailResponse
    {
        public bool Success { get; set; }
        public UserDetailData? Data { get; set; }
    }

    public class UserDetailData
    {
        public User? User { get; set; }
    }

    public class PaginatedUsersResponse
    {
        public bool Success { get; set; }
        public List<User> Users { get; set; } = new List<User>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
        public string Message { get; set; } = string.Empty;
    }
}
