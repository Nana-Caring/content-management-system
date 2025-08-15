using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("Idnumber")]
        public string IdNumber { get; set; } = string.Empty;

        public string? Relation { get; set; }

        [JsonPropertyName("phoneNumber")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // Postal Address Properties
        [JsonPropertyName("postalAddressLine1")]
        [StringLength(100)]
        public string? PostalAddressLine1 { get; set; }

        [JsonPropertyName("postalAddressLine2")]
        [StringLength(100)]
        public string? PostalAddressLine2 { get; set; }

        [JsonPropertyName("postalCity")]
        [StringLength(50)]
        public string? PostalCity { get; set; }

        [JsonPropertyName("postalProvince")]
        [StringLength(50)]
        public string? PostalProvince { get; set; }

        [JsonPropertyName("postalCode")]
        [StringLength(10)]
        public string? PostalCode { get; set; }

        // Home Address Properties
        [JsonPropertyName("homeAddressLine1")]
        [StringLength(100)]
        public string? HomeAddressLine1 { get; set; }

        [JsonPropertyName("homeAddressLine2")]
        [StringLength(100)]
        public string? HomeAddressLine2 { get; set; }

        [JsonPropertyName("homeCity")]
        [StringLength(50)]
        public string? HomeCity { get; set; }

        [JsonPropertyName("homeProvince")]
        [StringLength(50)]
        public string? HomeProvince { get; set; }

        [JsonPropertyName("homeCode")]
        [StringLength(10)]
        public string? HomeCode { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // User management properties
        public bool IsBlocked { get; set; } = false;

        public DateTime? BlockedAt { get; set; }

        [StringLength(500)]
        public string? BlockReason { get; set; }

        public int? BlockedBy { get; set; } // References another User's ID

        [StringLength(20)]
        public string Status { get; set; } = "active"; // 'active', 'blocked', 'suspended', 'pending'

        // Suspension properties
        public DateTime? SuspendedAt { get; set; }

        public DateTime? SuspendedUntil { get; set; }

        [StringLength(500)]
        public string? SuspensionReason { get; set; }

        public int? SuspendedBy { get; set; } // References another User's ID

        // Computed properties for display
        public string FullName => $"{FirstName} {MiddleName} {Surname}".Trim().Replace("  ", " ");

        public string PostalAddressFull => 
            string.Join(", ", new[] { PostalAddressLine1, PostalAddressLine2, PostalCity, PostalProvince, PostalCode }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        public string HomeAddressFull => 
            string.Join(", ", new[] { HomeAddressLine1, HomeAddressLine2, HomeCity, HomeProvince, HomeCode }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        // For backward compatibility
        [StringLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        // Alias for Phone property used in KYC page
        public string Phone => PhoneNumber ?? string.Empty;

        // Navigation property for related accounts
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}