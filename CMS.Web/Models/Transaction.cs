using System;

namespace CMS.Web.Models
{
    /// <summary>
    /// Represents a financial transaction in the system.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the related account.
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Transaction amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Date and time of the transaction.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Type of transaction (e.g., "Credit" or "Debit").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description or note for the transaction.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Navigation property to the related account.
        /// </summary>
        public Account Account { get; set; }
    }
}