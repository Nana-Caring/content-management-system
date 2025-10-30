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
    public class EditModel : BasePageModel
    {
        private readonly IApiService _api;

        public EditModel(IAppStateManager stateManager, IApiService api) : base(stateManager)
        {
            _api = api;
        }

        [BindProperty]
        public int Id { get; set; }
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
        public int StockQuantity { get; set; } = 0;
        [BindProperty]
        public string? TagsText { get; set; }
        [BindProperty]
        public bool IsActive { get; set; } = true;
        [BindProperty]
        public int? MinAge { get; set; }
        [BindProperty]
        public int? MaxAge { get; set; }
        [BindProperty]
        public string? AgeCategory { get; set; } = "All Ages";
    [BindProperty]
    public bool RequiresAgeVerification { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["Title"] = "Edit Product";
            Id = id;
            var prod = await _api.GetAdminProductByIdOrSkuAsync(id.ToString());
            if (prod == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return RedirectToPage("./Index");
            }

            Name = prod.Name ?? string.Empty;
            Brand = prod.Brand ?? string.Empty;
            Price = prod.Price ?? "0";
            Category = prod.Category ?? string.Empty;
            Sku = prod.Sku;
            Description = prod.Description;
            DetailedDescription = prod.DetailedDescription;
            Image = prod.Image;
            ImagesText = prod.Images != null ? string.Join("\n", prod.Images) : null;
            InStock = prod.InStock ?? true;
            StockQuantity = prod.StockQuantity ?? 0;
            TagsText = prod.Tags != null ? string.Join(", ", prod.Tags) : null;
            IsActive = prod.IsActive ?? true;
            MinAge = prod.MinAge;
            MaxAge = prod.MaxAge;
            AgeCategory = prod.AgeCategory ?? "All Ages";
            RequiresAgeVerification = prod.RequiresAgeVerification ?? false;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var request = new AdminProductUpdateRequest
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

            var result = await _api.UpdateAdminProductAsync(id, request);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message ?? "Product updated successfully";
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Failed to update product");
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
