using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Models
{
    public class AdminLoginRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Jwt { get; set; } = string.Empty;
        public AdminUser? User { get; set; }
        
        // Helper properties for compatibility
        public bool Success => !string.IsNullOrEmpty(AccessToken) && User != null;
        public string Message { get; set; } = string.Empty;
        public AdminData? Data => User != null ? new AdminData 
        { 
            Id = User.Id.ToString(),
            Username = $"{User.FirstName} {User.Surname}".Trim(),
            Email = User.Email,
            Role = User.Role
        } : null;
        public string? Token => AccessToken;
    }

    public class AdminUser
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AdminData
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // API Response Models for User Management
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class BlockUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
    }

    public class UserStatsResponse
    {
        public bool Success { get; set; }
        public UserStatsData? Data { get; set; }
    }

    public class UserStatsData
    {
        public UserTotals Totals { get; set; } = new();
        public UserByRole ByRole { get; set; } = new();
        public UserRecent Recent { get; set; } = new();
    }

    public class UserTotals
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BlockedUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int PendingUsers { get; set; }
    }

    public class UserByRole
    {
        public int Funders { get; set; }
        public int Caregivers { get; set; }
        public int Dependents { get; set; }
    }

    public class UserRecent
    {
        public int Last30Days { get; set; }
    }
}
