using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Diagnostics;

namespace CMS.Web.Pages.Debug
{
    public class ApiTestModel : BasePageModel
    {
        private readonly IApiService _apiService;

        public ApiTestModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public List<User>? Users { get; set; }
        public int UserCount { get; set; }
        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? RawResponse { get; set; }
        public long ResponseTime { get; set; }
        public DateTime? LastTestTime { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "API Connection Test";
            await TestConnection();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await TestConnection();
            return Page();
        }

        private async Task TestConnection()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                LastTestTime = DateTime.Now;
                
                // Test basic connectivity first
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    var response = await httpClient.GetAsync("https://nanacaring-backend.onrender.com/admin/users");
                    RawResponse = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Try to get users through the API service
                        Users = await _apiService.GetUsersAsync();
                        UserCount = Users?.Count ?? 0;
                        IsConnected = Users != null && Users.Any();
                        
                        if (IsConnected)
                        {
                            SuccessMessage = $"Successfully connected to API and retrieved {UserCount} users.";
                        }
                        else
                        {
                            ErrorMessage = "Connected to API but no users returned or authentication failed.";
                        }
                    }
                    else
                    {
                        IsConnected = false;
                        ErrorMessage = $"API returned error: {response.StatusCode} - {response.ReasonPhrase}";
                    }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                IsConnected = false;
                ErrorMessage = "Connection timeout - the backend service may be in cold start mode. Please wait 30 seconds and try again.";
            }
            catch (HttpRequestException ex)
            {
                IsConnected = false;
                ErrorMessage = $"Network error: {ex.Message}";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                ResponseTime = stopwatch.ElapsedMilliseconds;
            }
        }
    }
}
