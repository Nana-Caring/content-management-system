using CMS.Web.Models.State;

namespace CMS.Web.Services
{
    public interface IAppStateManager
    {
        // User State Management
        Task SetUserInfoAsync(UserInfo userInfo);
        Task<UserInfo> GetUserInfoAsync();
        Task ClearUserInfoAsync();
        Task UpdateUserPreferenceAsync(string key, object value);
        
        // Navigation State Management
        Task SetCurrentPageAsync(string pageName);
        Task<NavigationState> GetNavigationStateAsync();
        Task AddToBreadcrumbAsync(string page, object data);
        
        // Notification Management
        Task AddNotificationAsync(NotificationItem notification);
        Task AddNotificationAsync(string type, string title, string message, bool isPersistent = false);
        Task<NotificationState> GetNotificationsAsync();
        Task MarkNotificationAsReadAsync(string notificationId);
        Task ClearNotificationsAsync();
        
        // Loading State Management
        Task SetGlobalLoadingAsync(bool isLoading, string? message = null);
        Task SetComponentLoadingAsync(string componentKey, bool isLoading, string? message = null);
        Task<LoadingState> GetLoadingStateAsync();
        
        // Form State Management
        Task SaveFormDataAsync(string formKey, object formData);
        Task<T?> GetFormDataAsync<T>(string formKey) where T : class;
        Task ClearFormDataAsync(string formKey);
        
        // Search and Pagination
        Task SaveSearchFiltersAsync(string pageKey, object filters);
        Task<T?> GetSearchFiltersAsync<T>(string pageKey) where T : class;
        Task SavePaginationStateAsync(string pageKey, int page, int pageSize);
        Task<(int page, int pageSize)> GetPaginationStateAsync(string pageKey);
        
        // General State
        Task<AppState> GetAppStateAsync();
        Task ClearAllStateAsync();
        
        // Debug Methods
        Task<string> GetStateDebugInfoAsync();
        Task LogCurrentStateAsync(string context = "");
    }

    public class AppStateManager : IAppStateManager
    {
        private readonly IStateService _stateService;
        private readonly ILogger<AppStateManager> _logger;

        public AppStateManager(IStateService stateService, ILogger<AppStateManager> logger)
        {
            _stateService = stateService;
            _logger = logger;
        }

        // User State Management
        public async Task SetUserInfoAsync(UserInfo userInfo)
        {
            userInfo.LastActivity = DateTime.UtcNow;
            _stateService.SetState(StateKeys.USER_STATE, userInfo);
            _logger.LogDebug($"User info set for user: {userInfo.Username}");
            await Task.CompletedTask;
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            var userInfo = _stateService.GetState<UserInfo>(StateKeys.USER_STATE);
            return await Task.FromResult(userInfo ?? new UserInfo());
        }

        public async Task ClearUserInfoAsync()
        {
            _stateService.RemoveState(StateKeys.USER_STATE);
            _logger.LogDebug("User info cleared");
            await Task.CompletedTask;
        }

        public async Task UpdateUserPreferenceAsync(string key, object value)
        {
            var userInfo = await GetUserInfoAsync();
            userInfo.Preferences[key] = value;
            await SetUserInfoAsync(userInfo);
        }

        // Navigation State Management
        public async Task SetCurrentPageAsync(string pageName)
        {
            var navState = await GetNavigationStateAsync();
            navState.PreviousPage = navState.CurrentPage;
            navState.CurrentPage = pageName;
            
            if (!string.IsNullOrEmpty(pageName))
            {
                navState.NavigationHistory.Add(pageName);
                if (navState.NavigationHistory.Count > 10) // Keep only last 10 pages
                {
                    navState.NavigationHistory.RemoveAt(0);
                }
            }
            
            _stateService.SetState(StateKeys.NAVIGATION_STATE, navState);
        }

        public async Task<NavigationState> GetNavigationStateAsync()
        {
            var navState = _stateService.GetState<NavigationState>(StateKeys.NAVIGATION_STATE);
            return await Task.FromResult(navState ?? new NavigationState());
        }

        public async Task AddToBreadcrumbAsync(string page, object data)
        {
            var navState = await GetNavigationStateAsync();
            navState.BreadcrumbData[page] = data;
            _stateService.SetState(StateKeys.NAVIGATION_STATE, navState);
        }

        // Notification Management
        public async Task AddNotificationAsync(NotificationItem notification)
        {
            var notificationState = await GetNotificationsAsync();
            notificationState.Items.Add(notification);
            notificationState.UnreadCount++;
            
            _stateService.SetState(StateKeys.NOTIFICATIONS_STATE, notificationState);
            _logger.LogDebug($"Notification added: {notification.Title}");
        }

        public async Task AddNotificationAsync(string type, string title, string message, bool isPersistent = false)
        {
            var notification = new NotificationItem
            {
                Type = type,
                Title = title,
                Message = message,
                IsPersistent = isPersistent
            };
            await AddNotificationAsync(notification);
        }

        public async Task<NotificationState> GetNotificationsAsync()
        {
            var notificationState = _stateService.GetState<NotificationState>(StateKeys.NOTIFICATIONS_STATE);
            return await Task.FromResult(notificationState ?? new NotificationState());
        }

        public async Task MarkNotificationAsReadAsync(string notificationId)
        {
            var notificationState = await GetNotificationsAsync();
            var notification = notificationState.Items.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notificationState.UnreadCount = Math.Max(0, notificationState.UnreadCount - 1);
                _stateService.SetState(StateKeys.NOTIFICATIONS_STATE, notificationState);
            }
        }

        public async Task ClearNotificationsAsync()
        {
            var notificationState = new NotificationState();
            _stateService.SetState(StateKeys.NOTIFICATIONS_STATE, notificationState);
            await Task.CompletedTask;
        }

        // Loading State Management
        public async Task SetGlobalLoadingAsync(bool isLoading, string? message = null)
        {
            var loadingState = await GetLoadingStateAsync();
            loadingState.IsGlobalLoading = isLoading;
            if (message != null)
            {
                loadingState.LoadingMessages["global"] = message;
            }
            _stateService.SetState(StateKeys.LOADING_STATE, loadingState);
        }

        public async Task SetComponentLoadingAsync(string componentKey, bool isLoading, string? message = null)
        {
            var loadingState = await GetLoadingStateAsync();
            loadingState.ComponentLoading[componentKey] = isLoading;
            if (message != null)
            {
                loadingState.LoadingMessages[componentKey] = message;
            }
            _stateService.SetState(StateKeys.LOADING_STATE, loadingState);
        }

        public async Task<LoadingState> GetLoadingStateAsync()
        {
            var loadingState = _stateService.GetState<LoadingState>(StateKeys.LOADING_STATE);
            return await Task.FromResult(loadingState ?? new LoadingState());
        }

        // Form State Management
        public async Task SaveFormDataAsync(string formKey, object formData)
        {
            var key = $"{StateKeys.FORM_DATA}_{formKey}";
            _stateService.SetState(key, formData);
            await Task.CompletedTask;
        }

        public async Task<T?> GetFormDataAsync<T>(string formKey) where T : class
        {
            var key = $"{StateKeys.FORM_DATA}_{formKey}";
            var formData = _stateService.GetState<T>(key);
            return await Task.FromResult(formData);
        }

        public async Task ClearFormDataAsync(string formKey)
        {
            var key = $"{StateKeys.FORM_DATA}_{formKey}";
            _stateService.RemoveState(key);
            await Task.CompletedTask;
        }

        // Search and Pagination
        public async Task SaveSearchFiltersAsync(string pageKey, object filters)
        {
            var key = $"{StateKeys.SEARCH_FILTERS}_{pageKey}";
            _stateService.SetState(key, filters);
            await Task.CompletedTask;
        }

        public async Task<T?> GetSearchFiltersAsync<T>(string pageKey) where T : class
        {
            var key = $"{StateKeys.SEARCH_FILTERS}_{pageKey}";
            var filters = _stateService.GetState<T>(key);
            return await Task.FromResult(filters);
        }

        public async Task SavePaginationStateAsync(string pageKey, int page, int pageSize)
        {
            var key = $"{StateKeys.PAGINATION_STATE}_{pageKey}";
            var paginationData = new { page, pageSize };
            _stateService.SetState(key, paginationData);
            await Task.CompletedTask;
        }

        public async Task<(int page, int pageSize)> GetPaginationStateAsync(string pageKey)
        {
            var key = $"{StateKeys.PAGINATION_STATE}_{pageKey}";
            var paginationData = _stateService.GetState<dynamic>(key);
            if (paginationData != null)
            {
                return await Task.FromResult(((int)paginationData.page, (int)paginationData.pageSize));
            }
            return await Task.FromResult((1, 10)); // Default values
        }

        // General State
        public async Task<AppState> GetAppStateAsync()
        {
            return new AppState
            {
                UserInfo = await GetUserInfoAsync(),
                Navigation = await GetNavigationStateAsync(),
                Notifications = await GetNotificationsAsync(),
                Loading = await GetLoadingStateAsync()
            };
        }

        public async Task ClearAllStateAsync()
        {
            _stateService.ClearState();
            _logger.LogInformation("All application state cleared");
            await Task.CompletedTask;
        }

        // Debug Methods
        public async Task<string> GetStateDebugInfoAsync()
        {
            var appState = await GetAppStateAsync();
            var debugInfo = new
            {
                Timestamp = DateTime.UtcNow,
                User = new
                {
                    IsAuthenticated = appState.UserInfo?.IsAuthenticated ?? false,
                    Email = appState.UserInfo?.Email ?? "N/A",
                    Role = appState.UserInfo?.Role ?? "N/A",
                    LastActivity = appState.UserInfo?.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                },
                Navigation = new
                {
                    CurrentPage = appState.Navigation.CurrentPage ?? "N/A",
                    PreviousPage = appState.Navigation.PreviousPage ?? "N/A",
                    HistoryCount = appState.Navigation.NavigationHistory.Count
                },
                Notifications = new
                {
                    Total = appState.Notifications.Items.Count,
                    Unread = appState.Notifications.UnreadCount,
                    Recent = appState.Notifications.Items.Take(3).Select(n => new { n.Type, n.Title, n.CreatedAt })
                },
                Loading = new
                {
                    IsGlobalLoading = appState.Loading.IsGlobalLoading,
                    ComponentsLoading = appState.Loading.ComponentLoading.Count,
                    ActiveComponents = appState.Loading.ComponentLoading.Where(kvp => kvp.Value).Select(kvp => kvp.Key)
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(debugInfo, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        public async Task LogCurrentStateAsync(string context = "")
        {
            var debugInfo = await GetStateDebugInfoAsync();
            _logger.LogInformation($"[STATE DEBUG{(string.IsNullOrEmpty(context) ? "" : $" - {context}")}] Current Application State:\n{debugInfo}");
        }
    }
}
