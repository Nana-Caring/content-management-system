using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Services
{
    public interface IProductService
    {
        Task<List<AdminProduct>> GetProductsAsync();
        Task<AdminProduct?> GetProductByIdAsync(int id);
        Task<AdminProduct> CreateProductAsync(AdminProductCreateRequest request);
        Task<AdminProduct> UpdateProductAsync(int id, AdminProductUpdateRequest request);
        Task<bool> DeleteProductAsync(int id);
        Task SyncWithExternalApiAsync();
    }

    public class ProductService : IProductService
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IApiService apiService, ILogger<ProductService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<List<AdminProduct>> GetProductsAsync()
        {
            try
            {
                // For now, get directly from external API
                // TODO: Add local caching when database is properly set up
                var (apiProducts, _) = await _apiService.GetAdminProductsAsync();
                return apiProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products from API");
                return new List<AdminProduct>();
            }
        }

        public async Task<AdminProduct?> GetProductByIdAsync(int id)
        {
            return await _apiService.GetAdminProductByIdOrSkuAsync(id.ToString());
        }

        public async Task<AdminProduct> CreateProductAsync(AdminProductCreateRequest request)
        {
            try
            {
                // Create directly in external API - it persists automatically
                var result = await _apiService.CreateAdminProductAsync(request);
                
                if (result.Success && result.Data != null)
                {
                    _logger.LogInformation("Product created and persisted to external API");
                    return result.Data;
                }
                else
                {
                    throw new Exception($"Failed to create product: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<AdminProduct> UpdateProductAsync(int id, AdminProductUpdateRequest request)
        {
            try
            {
                // Update directly in external API - it persists automatically
                var result = await _apiService.UpdateAdminProductAsync(id, request);
                
                if (result.Success && result.Data != null)
                {
                    _logger.LogInformation("Product updated and persisted to external API");
                    return result.Data;
                }
                else
                {
                    throw new Exception($"Failed to update product: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                // Delete directly from external API - it persists automatically
                var result = await _apiService.DeleteAdminProductAsync(id);
                
                if (result.Success)
                {
                    _logger.LogInformation("Product deleted and persisted to external API");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to delete product: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return false;
            }
        }

        public async Task SyncWithExternalApiAsync()
        {
            // Not implemented - we're working directly with external API for now
            // TODO: Implement local caching/sync when database is properly set up
            _logger.LogInformation("Sync not needed - working directly with external API");
            await Task.CompletedTask;
        }
    }
}