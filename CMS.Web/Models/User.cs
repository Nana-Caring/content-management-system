using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("middleName")]
        public string MiddleName { get; set; } = string.Empty;

        [JsonPropertyName("surname")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("Idnumber")]
        public string IdNumber { get; set; } = string.Empty;

        public string? Relation { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // Computed property for display
        public string FullName => $"{FirstName} {MiddleName} {Surname}".Trim().Replace("  ", " ");

        // For backward compatibility
        [StringLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        // Alias for Phone property used in KYC page
        public string Phone => PhoneNumber;

        // Navigation property for related accounts
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}