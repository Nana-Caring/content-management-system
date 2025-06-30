using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Models
{
    public class KycRequest
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        public User User { get; set; } = null!;
        
        [Required]
        public string DocumentType { get; set; } = string.Empty;
        
        [Required]
        public string DocumentUrl { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
        
        public DateTime SubmittedAt { get; set; }
    }
}