using System.Text.Json.Serialization;

namespace CMS.Web.Models
{
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int Pages { get; set; }

        // Some backends return `totalPages` instead of `pages`
        [JsonPropertyName("totalPages")] 
        public int TotalPages { get; set; }
    }
}
