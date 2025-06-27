using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Models
{
    /// <summary>
    /// Represents a financial account in the system.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique account number.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Name of the account.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string AccountName { get; set; }

        /// <summary>
        /// Current balance of the account.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Balance { get; set; }

        /// <summary>
        /// Type of the account (e.g., Savings, Checking).
        /// </summary>
        [StringLength(50)]
        public string AccountType { get; set; }

        /// <summary>
        /// Date and time the account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for related transactions.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; }
    }
}