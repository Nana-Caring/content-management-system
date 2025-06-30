using CMS.Web.Models.State;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace CMS.Web.Services
{
    public class LocalStorageStateService : IStateService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<LocalStorageStateService> _logger;

        public LocalStorageStateService(IJSRuntime jsRuntime, ILogger<LocalStorageStateService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<T?> GetStateAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(value))
                    return null;

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting state from localStorage for key: {key}");
                return null;
            }
        }

        public async Task SetStateAsync<T>(string key, T value) where T : class
        {
            try
            {
                var serializedValue = JsonConvert.SerializeObject(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting state in localStorage for key: {key}");
            }
        }

        public async Task RemoveStateAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing state from localStorage for key: {key}");
            }
        }

        public async Task ClearAllStateAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.clear");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing localStorage");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if key exists in localStorage: {key}");
                return false;
            }
        }

        // Sync methods for compatibility - these will use async internally
        public T? GetState<T>(string key) where T : class
        {
            try
            {
                // For sync methods, we'll fall back to a simple in-memory cache
                // This is not ideal but necessary for compatibility
                return GetStateAsync<T>(key).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting state synchronously for key: {key}");
                return default(T);
            }
        }

        public void SetState<T>(string key, T value) where T : class
        {
            try
            {
                SetStateAsync(key, value).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting state synchronously for key: {key}");
            }
        }

        public void RemoveState(string key)
        {
            try
            {
                RemoveStateAsync(key).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing state synchronously for key: {key}");
            }
        }

        public void ClearState()
        {
            try
            {
                ClearAllStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing state synchronously");
            }
        }

        public bool HasState(string key)
        {
            try
            {
                return ExistsAsync(key).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking state synchronously for key: {key}");
                return false;
            }
        }
    }
}
