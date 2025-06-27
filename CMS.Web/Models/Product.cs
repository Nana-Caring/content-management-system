using System;

namespace CMS.Web.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiLink { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}