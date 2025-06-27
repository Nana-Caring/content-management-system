using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Services;
using CMS.Web.Models.State;
using System.Security.Claims;

namespace CMS.Web.Models
{
    public abstract class BasePageModel : PageModel
    {
        protected readonly IAppStateManager _stateManager;

        public UserInfo CurrentUser { get; private set; } = new();
        public NavigationState Navigation { get; private set; } = new();
        public LoadingState Loading { get; private set; } = new();
        public NotificationState Notifications { get; private set; } = new();

        protected BasePageModel(IAppStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            // Load current state before page execution
            await LoadCurrentState();
            
            // Set current page in navigation state
            var pageName = GetPageName();
            await _stateManager.SetCurrentPageAsync(pageName);
            
            // Update user state from claims if authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                await UpdateUserStateFromClaims();
            }

            // Make AppState available in ViewData for the layout
            ViewData["AppState"] = await _stateManager.GetAppStateAsync();

            await next();
        }

        protected virtual async Task LoadCurrentState()
        {
            CurrentUser = await _stateManager.GetUserInfoAsync();
            Navigation = await _stateManager.GetNavigationStateAsync();
            Loading = await _stateManager.GetLoadingStateAsync();
            Notifications = await _stateManager.GetNotificationsAsync();
        }

        protected virtual string GetPageName()
        {
            var pagePath = HttpContext.Request.Path.Value ?? "";
            return pagePath.Trim('/') ?? "Home";
        }

        protected virtual async Task UpdateUserStateFromClaims()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userInfo = new UserInfo
                {
                    IsAuthenticated = true,
                    UserId = User.FindFirst("UserId")?.Value ?? "",
                    Username = User.FindFirst(ClaimTypes.Name)?.Value ?? "",
                    Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "",
                    Token = User.FindFirst("Token")?.Value,
                    LastActivity = DateTime.UtcNow
                };

                await _stateManager.SetUserInfoAsync(userInfo);
                CurrentUser = userInfo;
            }
        }

        // Helper methods for common state operations
        protected async Task SetLoading(bool isLoading, string? message = null)
        {
            await _stateManager.SetGlobalLoadingAsync(isLoading, message);
            Loading = await _stateManager.GetLoadingStateAsync();
        }

        protected async Task AddNotification(string type, string title, string message)
        {
            await _stateManager.AddNotificationAsync(type, title, message);
            Notifications = await _stateManager.GetNotificationsAsync();
        }

        // Method to get state as JSON for client-side use
        protected async Task<string> GetStateAsJson()
        {
            var appState = await _stateManager.GetAppStateAsync();
            return System.Text.Json.JsonSerializer.Serialize(appState, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
    }
}
