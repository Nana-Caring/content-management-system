using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Pages;

[Authorize]
public class IndexModel : BasePageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IApiService _apiService;

    public string WelcomeMessage { get; set; } = "";
    public DashboardStats Stats { get; set; } = new DashboardStats();
    public string? ErrorMessage { get; set; }

    public IndexModel(IAppStateManager stateManager, ILogger<IndexModel> logger, IApiService apiService) : base(stateManager)
    {
        _logger = logger;
        _apiService = apiService;
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Dashboard";
        
        try
        {
            // Set loading state
            await SetLoading(true, "Loading dashboard data...");

            // Set welcome message from user state
            WelcomeMessage = !string.IsNullOrEmpty(CurrentUser.Username) 
                ? $"Welcome back, {CurrentUser.Username}!" 
                : "Welcome to CMS Dashboard!";

            // Load dashboard data from API
            await LoadDashboardData();

            // Add welcome notification for first-time users
            var lastLoginPref = CurrentUser.Preferences.ContainsKey("lastLogin") 
                ? CurrentUser.Preferences["lastLogin"] 
                : null;

            if (lastLoginPref == null)
            {
                await AddNotification("info", "Welcome!", "Welcome to the CMS Dashboard. Explore the features using the sidebar menu.");
                await _stateManager.UpdateUserPreferenceAsync("lastLogin", DateTime.UtcNow);
            }

            // Clear loading state
            await SetLoading(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            ErrorMessage = "Failed to load dashboard data. Please refresh the page.";
            await AddNotification("error", "Error", "Failed to load dashboard data. Please refresh the page.");
            await SetLoading(false);
        }
    }

    private async Task LoadDashboardData()
    {
        try
        {
            // Load stats from the API
            Stats = await _apiService.GetDashboardStatsAsync();
            
            _logger.LogInformation($"Dashboard data loaded for user: {CurrentUser.Username}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard stats from API");
            // Use default/empty stats on error
            Stats = new DashboardStats();
            throw; // Re-throw to be handled by the calling method
        }
    }
}
