using System;

namespace CMS.Web.Models
{
    public class KycRequest
    {
        public int Id { get; set; }
        public User User { get; set; }
        public string DocumentType { get; set; }
        public string DocumentUrl { get; set; }
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
        public DateTime SubmittedAt { get; set; }
    }
}