namespace CMS.Web.Models.State
{
    public class AppState
    {
        public UserState User { get; set; } = new();
        public NavigationState Navigation { get; set; } = new();
        public NotificationState Notifications { get; set; } = new();
        public LoadingState Loading { get; set; } = new();
    }

    public class UserState
    {
        public bool IsAuthenticated { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, object> Preferences { get; set; } = new();
    }

    public class NavigationState
    {
        public string? CurrentPage { get; set; }
        public string? PreviousPage { get; set; }
        public Dictionary<string, object> BreadcrumbData { get; set; } = new();
        public List<string> NavigationHistory { get; set; } = new();
    }

    public class NotificationState
    {
        public List<NotificationItem> Items { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "info"; // success, error, warning, info
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public bool IsPersistent { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class LoadingState
    {
        public bool IsGlobalLoading { get; set; }
        public Dictionary<string, bool> ComponentLoading { get; set; } = new();
        public Dictionary<string, string> LoadingMessages { get; set; } = new();
    }

    // State keys for session storage
    public static class StateKeys
    {
        public const string APP_STATE = "APP_STATE";
        public const string USER_STATE = "USER_STATE";
        public const string NAVIGATION_STATE = "NAVIGATION_STATE";
        public const string NOTIFICATIONS_STATE = "NOTIFICATIONS_STATE";
        public const string LOADING_STATE = "LOADING_STATE";
        public const string FORM_DATA = "FORM_DATA";
        public const string SEARCH_FILTERS = "SEARCH_FILTERS";
        public const string PAGINATION_STATE = "PAGINATION_STATE";
    }
}
