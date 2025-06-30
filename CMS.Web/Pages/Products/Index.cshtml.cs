using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Pages.Products
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<Product> Products { get; set; } = new List<Product>();
        public string? ErrorMessage { get; set; }
        
        // Filter properties
        public string? SearchTerm { get; set; }
        public string? DateRange { get; set; }
        public string? SortBy { get; set; }
        
        // Sort properties
        public string SortField { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync(string? search, string? dateRange, string? sortBy, 
            string? sortField, string? sortDirection)
        {
            ViewData["Title"] = "Products Management";
            
            // Set filter parameters
            SearchTerm = search;
            DateRange = dateRange;
            SortBy = sortBy;
            SortField = sortField ?? sortBy ?? "CreatedAt";
            SortDirection = sortDirection ?? "desc";
            
            try
            {
                var allProducts = await _apiService.GetProductsAsync();
                
                // Convert to list first for consistency
                var productsList = allProducts.ToList();
                
                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    productsList = productsList.Where(p => 
                        (p.Name != null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Description != null && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (p.ApiLink != null && p.ApiLink.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        p.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(dateRange))
                {
                    var now = DateTime.Now;
                    productsList = dateRange.ToLower() switch
                    {
                        "today" => productsList.Where(p => p.CreatedAt.Date == now.Date).ToList(),
                        "week" => productsList.Where(p => p.CreatedAt >= now.AddDays(-7)).ToList(),
                        "month" => productsList.Where(p => p.CreatedAt >= now.AddMonths(-1)).ToList(),
                        "year" => productsList.Where(p => p.CreatedAt >= now.AddYears(-1)).ToList(),
                        _ => productsList
                    };
                }
                
                // Apply sorting
                productsList = SortField.ToLower() switch
                {
                    "id" => SortDirection == "asc" 
                        ? productsList.OrderBy(p => p.Id).ToList()
                        : productsList.OrderByDescending(p => p.Id).ToList(),
                    "name" => SortDirection == "asc" 
                        ? productsList.OrderBy(p => p.Name).ToList()
                        : productsList.OrderByDescending(p => p.Name).ToList(),
                    "createdat" or "created" => SortDirection == "asc" 
                        ? productsList.OrderBy(p => p.CreatedAt).ToList()
                        : productsList.OrderByDescending(p => p.CreatedAt).ToList(),
                    _ => productsList.OrderByDescending(p => p.CreatedAt).ToList()
                };
                
                Products = productsList;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load products. Please try again later.";
                // Log the exception for debugging
                Console.WriteLine($"Error loading products: {ex.Message}");
            }
        }
        
        public string GetNextSortDirection(string field)
        {
            if (SortField?.Equals(field, StringComparison.OrdinalIgnoreCase) == true)
            {
                return SortDirection == "asc" ? "desc" : "asc";
            }
            return "asc";
        }
    }
}