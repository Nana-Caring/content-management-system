using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Web.Models
{
    /// <summary>
    /// Represents a financial account in the system.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Primary key (UUID from backend).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User ID that owns this account.
        /// </summary>
        [JsonPropertyName("userId")]
        public int? UserId { get; set; }

        /// <summary>
        /// Type of the account (e.g., Main, Education, Healthcare, etc.).
        /// </summary>
        [JsonPropertyName("accountType")]
        public string AccountType { get; set; } = string.Empty;

        /// <summary>
        /// Unique account number.
        /// </summary>
        [Required]
        [StringLength(20)]
        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Current balance of the account.
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Currency code (e.g., ZAR).
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Account status (e.g., active, inactive).
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Date when the account was created (string format from backend).
        /// </summary>
        [JsonPropertyName("creationDate")]
        public string CreationDate { get; set; } = string.Empty;

        /// <summary>
        /// Date of last transaction.
        /// </summary>
        [JsonPropertyName("lastTransactionDate")]
        public string? LastTransactionDate { get; set; }

        /// <summary>
        /// Parent account ID for sub-accounts.
        /// </summary>
        [JsonPropertyName("parentAccountId")]
        public string? ParentAccountId { get; set; }

        /// <summary>
        /// Date and time the account was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time the account was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Navigation property for related transactions.
        /// </summary>
        [JsonIgnore]
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        /// <summary>
        /// Computed property for display name.
        /// </summary>
        [JsonIgnore]
        public string AccountName => $"{AccountType} Account";

        /// <summary>
        /// Computed property to check if it's a main account.
        /// </summary>
        [JsonIgnore]
        public bool IsMainAccount => string.IsNullOrEmpty(ParentAccountId);
    }
}