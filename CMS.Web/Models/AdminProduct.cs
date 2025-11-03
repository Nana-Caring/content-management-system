using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CMS.Web.Models.Converters;

namespace CMS.Web.Models
{
    public class AdminProduct
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("brand")] public string? Brand { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("detailedDescription")] public string? DetailedDescription { get; set; }
        // Backend may send price as string or number; use converter to handle both
        [JsonPropertyName("price")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal? Price { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("subcategory")] public string? Subcategory { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
    [JsonPropertyName("images")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public List<string>? Images { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("stockQuantity")] public int? StockQuantity { get; set; }
        [JsonPropertyName("ingredients")] public string? Ingredients { get; set; }
        [JsonPropertyName("weight")] public string? Weight { get; set; }
        [JsonPropertyName("manufacturer")] public string? Manufacturer { get; set; }
    [JsonPropertyName("tags")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public List<string>? Tags { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
        [JsonPropertyName("minAge")] public int? MinAge { get; set; }
        [JsonPropertyName("maxAge")] public int? MaxAge { get; set; }
        [JsonPropertyName("ageCategory")] public string? AgeCategory { get; set; }
        [JsonPropertyName("requiresAgeVerification")] public bool? RequiresAgeVerification { get; set; }
        [JsonPropertyName("createdBy")] public int? CreatedBy { get; set; }
        [JsonPropertyName("updatedBy")] public int? UpdatedBy { get; set; }
        [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime? UpdatedAt { get; set; }
    }

    public class AdminProductListResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public List<AdminProduct>? Data { get; set; }
        [JsonPropertyName("pagination")] public PaginationInfo? Pagination { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    public class AdminProductResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("data")] public AdminProduct? Data { get; set; }
    }

    public class AdminProductCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        // Send as numeric to satisfy backend validation
        public decimal? Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Subcategory { get; set; }
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string? DetailedDescription { get; set; }
        public string? Image { get; set; }
        public List<string>? Images { get; set; }
        public bool? InStock { get; set; }
        public int? StockQuantity { get; set; }
        public string? Ingredients { get; set; }
        public string? Weight { get; set; }
        public string? Manufacturer { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsActive { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? AgeCategory { get; set; }
        public bool? RequiresAgeVerification { get; set; }
    }

    public class AdminProductUpdateRequest : AdminProductCreateRequest {}
}
