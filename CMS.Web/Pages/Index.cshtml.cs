using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Pages;

[Authorize]
public class IndexModel : BasePageModel
{
    private readonly ILogger<IndexModel> _logger;

    public string WelcomeMessage { get; set; } = "";
    public int TotalUsers { get; set; }
    public int TotalAccounts { get; set; }
    public int PendingKyc { get; set; }
    public decimal TotalTransactions { get; set; }

    public IndexModel(IAppStateManager stateManager, ILogger<IndexModel> logger) : base(stateManager)
    {
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Set loading state
            await SetLoading(true, "Loading dashboard data...");

            // Set welcome message from user state
            WelcomeMessage = !string.IsNullOrEmpty(CurrentUser.Username) 
                ? $"Welcome back, {CurrentUser.Username}!" 
                : "Welcome to CMS Dashboard!";

            // Load dashboard data (replace with actual data loading)
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
            await AddNotification("error", "Error", "Failed to load dashboard data. Please refresh the page.");
            await SetLoading(false);
        }
    }

    private async Task LoadDashboardData()
    {
        // Simulate loading data - replace with actual database calls
        await Task.Delay(500); // Simulate API delay

        // Sample data - replace with actual queries
        TotalUsers = 1250;
        TotalAccounts = 890;
        PendingKyc = 45;
        TotalTransactions = 125000.50m;

        _logger.LogInformation($"Dashboard data loaded for user: {CurrentUser.Username}");
    }
}
