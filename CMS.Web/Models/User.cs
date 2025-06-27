using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for related accounts
        public ICollection<Account> Accounts { get; set; }
    }
}