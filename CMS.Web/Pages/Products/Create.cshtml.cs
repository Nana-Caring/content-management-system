using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CMS.Web.Models;
using CMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Pages.Products
{
    [Authorize]
    public class CreateModel : BasePageModel
    {
        private readonly IApiService _api;

        public CreateModel(IAppStateManager stateManager, IApiService api) : base(stateManager)
        {
            _api = api;
        }

        [BindProperty, Required]
        public string Name { get; set; } = string.Empty;
        [BindProperty]
        public string Brand { get; set; } = string.Empty;
        [BindProperty]
        public string Price { get; set; } = "0";
        [BindProperty]
        public string Category { get; set; } = string.Empty;
        [BindProperty]
        public string? Sku { get; set; }
        [BindProperty]
        public string? Description { get; set; }
        [BindProperty]
        public string? DetailedDescription { get; set; }
        [BindProperty]
        public string? Image { get; set; }
        [BindProperty]
        public string? ImagesText { get; set; }
        [BindProperty]
        public bool InStock { get; set; } = true;
        [BindProperty]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0;
        [BindProperty]
        public string? TagsText { get; set; }
        [BindProperty]
        public bool IsActive { get; set; } = true;
        [BindProperty]
        [Range(0, 150)]
        public int? MinAge { get; set; }
        [BindProperty]
        [Range(0, 150)]
        public int? MaxAge { get; set; }
        [BindProperty]
        public string? AgeCategory { get; set; } = "All Ages";
    [BindProperty]
    public bool RequiresAgeVerification { get; set; }

        public void OnGet()
        {
            ViewData["Title"] = "Add Product";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new AdminProductCreateRequest
            {
                Name = Name,
                Brand = Brand,
                Price = Price,
                Category = Category,
                Sku = Sku,
                Description = Description,
                DetailedDescription = DetailedDescription,
                Image = Image,
                Images = ParseList(ImagesText),
                InStock = InStock,
                StockQuantity = StockQuantity,
                Tags = ParseList(TagsText),
                IsActive = IsActive,
                MinAge = MinAge,
                MaxAge = MaxAge,
                AgeCategory = AgeCategory,
                RequiresAgeVerification = RequiresAgeVerification
            };

            var result = await _api.CreateAdminProductAsync(request);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message ?? "Product created successfully";
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Failed to create product");
            return Page();
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
