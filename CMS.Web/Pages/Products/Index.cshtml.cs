using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System;
using CMS.Web.Models;

namespace CMS.Web.Pages.Products
{
    public class IndexModel : PageModel
    {
        public List<Product> Products { get; set; }

        public void OnGet()
        {
            // Replace this with your actual data retrieval logic (e.g., from a database)
            Products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Sample Product 1",
                    ApiLink = "https://api.example.com/product1",
                    Description = "This is a sample product.",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 2,
                    Name = "Sample Product 2",
                    ApiLink = "https://api.example.com/product2",
                    Description = "Another sample product.",
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
    }
}