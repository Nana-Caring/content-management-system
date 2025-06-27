using Microsoft.AspNetCore.Authorization;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System;

namespace CMS.Web.Pages.Products
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        public List<Product> Products { get; set; }

        public IndexModel(IAppStateManager stateManager) : base(stateManager)
        {
        }

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