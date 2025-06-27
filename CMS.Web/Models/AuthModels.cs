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
}
