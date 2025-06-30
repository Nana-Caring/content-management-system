using CMS.Web.Models;
using System.Text;
using System.Text.Json;

namespace CMS.Web.Services
{
    public interface IApiService
    {
        // Users
        Task<List<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> DeleteUserAsync(int id);

        // Accounts
        Task<List<Account>> GetAccountsAsync();
        Task<Account?> GetAccountByIdAsync(int id);
        Task<bool> DeleteAccountAsync(int id);

        // Transactions
        Task<List<Transaction>> GetTransactionsAsync();
        Task<Transaction?> GetTransactionByIdAsync(int id);
        Task<bool> DeleteTransactionAsync(int id);

        // Products
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);

        // KYC Requests
        Task<List<KycRequest>> GetKycRequestsAsync();
        Task<KycRequest?> GetKycRequestByIdAsync(int id);

        // Dashboard Stats
        Task<DashboardStats> GetDashboardStatsAsync();
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
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            var users = await GetAsync<List<User>>("/admin/users");
            _logger.LogInformation($"GetUsersAsync completed - returned {(users?.Count ?? 0)} users");
            
            if (users != null && users.Any())
            {
                _logger.LogInformation($"Sample user data: ID={users[0].Id}, Name={users[0].FullName}, Email={users[0].Email}, Role={users[0].Role}");
            }
            
            return users ?? new List<User>();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await GetAsync<User>($"/admin/users/{id}");
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await DeleteAsync($"/admin/users/{id}");
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

        // Products
        public async Task<List<Product>> GetProductsAsync()
        {
            var products = await GetAsync<List<Product>>("/admin/products");
            return products ?? new List<Product>();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await GetAsync<Product>($"/admin/products/{id}");
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
    }

    // API Response Models
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
}
