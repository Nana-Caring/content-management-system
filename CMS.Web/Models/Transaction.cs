using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CMS.Web.Models
{
    /// <summary>
    /// Represents a financial transaction in the system.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Primary key (UUID from backend).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the related account (UUID).
        /// </summary>
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        /// <summary>
        /// Transaction amount.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Date and time of the transaction.
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Type of transaction (e.g., "deposit", "withdrawal", "transfer").
        /// </summary>
        [Required]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Description or note for the transaction.
        /// </summary>
        [Required]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Transaction status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        /// <summary>
        /// Date and time the transaction was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to the related account.
        /// </summary>
        [JsonIgnore]
        public Account? Account { get; set; }
    }
}