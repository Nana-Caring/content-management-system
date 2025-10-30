using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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

        // Handlers for modal forms
        public async Task<IActionResult> OnPostCreateAsync(
            [FromForm] string name,
            [FromForm] string brand,
            [FromForm] string price,
            [FromForm] string category,
            [FromForm] string? sku,
            [FromForm] string? description,
            [FromForm] string? detailedDescription,
            [FromForm] string? image,
            [FromForm] string? imagesText,
            [FromForm] bool inStock,
            [FromForm] int stockQuantity,
            [FromForm] string? tagsText,
            [FromForm] bool isActive,
            [FromForm] int? minAge,
            [FromForm] int? maxAge,
            [FromForm] string? ageCategory,
            [FromForm] bool requiresAgeVerification)
        {
            try
            {
                var request = new AdminProductCreateRequest
                {
                    Name = name,
                    Brand = brand,
                    Price = price,
                    Category = category,
                    Sku = sku,
                    Description = description,
                    DetailedDescription = detailedDescription,
                    Image = image,
                    Images = ParseList(imagesText),
                    InStock = inStock,
                    StockQuantity = stockQuantity,
                    Tags = ParseList(tagsText),
                    IsActive = isActive,
                    MinAge = minAge,
                    MaxAge = maxAge,
                    AgeCategory = ageCategory,
                    RequiresAgeVerification = requiresAgeVerification
                };

                var result = await _apiService.CreateAdminProductAsync(request);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Product created successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Failed to create product";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the product.";
                Console.WriteLine(ex);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            [FromForm] int id,
            [FromForm] string name,
            [FromForm] string brand,
            [FromForm] string price,
            [FromForm] string category,
            [FromForm] string? sku,
            [FromForm] string? description,
            [FromForm] string? detailedDescription,
            [FromForm] string? image,
            [FromForm] string? imagesText,
            [FromForm] bool inStock,
            [FromForm] int stockQuantity,
            [FromForm] string? tagsText,
            [FromForm] bool isActive,
            [FromForm] int? minAge,
            [FromForm] int? maxAge,
            [FromForm] string? ageCategory,
            [FromForm] bool requiresAgeVerification)
        {
            try
            {
                var request = new AdminProductUpdateRequest
                {
                    Name = name,
                    Brand = brand,
                    Price = price,
                    Category = category,
                    Sku = sku,
                    Description = description,
                    DetailedDescription = detailedDescription,
                    Image = image,
                    Images = ParseList(imagesText),
                    InStock = inStock,
                    StockQuantity = stockQuantity,
                    Tags = ParseList(tagsText),
                    IsActive = isActive,
                    MinAge = minAge,
                    MaxAge = maxAge,
                    AgeCategory = ageCategory,
                    RequiresAgeVerification = requiresAgeVerification
                };

                var result = await _apiService.UpdateAdminProductAsync(id, request);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Product updated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Failed to update product";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the product.";
                Console.WriteLine(ex);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] int id)
        {
            try
            {
                var result = await _apiService.DeleteAdminProductAsync(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message ?? "Product deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Failed to delete product";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the product.";
                Console.WriteLine(ex);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetProductJsonAsync(int id)
        {
            var prod = await _apiService.GetAdminProductByIdOrSkuAsync(id.ToString());
            if (prod == null) return new JsonResult(new { success = false, message = "Not found" }) { StatusCode = 404 };
            return new JsonResult(new { success = true, data = prod });
        }

        private static List<string>? ParseList(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var parts = input
                .Replace("\r", "")
                .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
            return parts.Count > 0 ? parts : null;
        }
    }
}