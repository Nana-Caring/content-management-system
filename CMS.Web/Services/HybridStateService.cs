using CMS.Web.Models.State;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace CMS.Web.Services
{
    public class HybridStateService : IStateService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<HybridStateService> _logger;

        public HybridStateService(
            IHttpContextAccessor httpContextAccessor, 
            IJSRuntime jsRuntime, 
            ILogger<HybridStateService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public async Task<T?> GetStateAsync<T>(string key) where T : class
        {
            try
            {
                // First try localStorage (persistent)
                try
                {
                    var localValue = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                    if (!string.IsNullOrEmpty(localValue))
                    {
                        var result = JsonConvert.DeserializeObject<T>(localValue);
                        if (result != null)
                        {
                            // Also store in session for faster access
                            await SetSessionAsync(key, result);
                            return result;
                        }
                    }
                }
                catch (Exception)
                {
                    // localStorage not available, fall back to session
                }

                // Fall back to session
                return await GetSessionAsync<T>(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting state for key: {key}");
                return null;
            }
        }

        public async Task SetStateAsync<T>(string key, T value) where T : class
        {
            try
            {
                // Store in both localStorage and session
                await SetSessionAsync(key, value);
                
                try
                {
                    var serializedValue = JsonConvert.SerializeObject(value);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, serializedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not store in localStorage for key: {key}, using session only");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting state for key: {key}");
            }
        }

        public async Task RemoveStateAsync(string key)
        {
            try
            {
                // Remove from both
                await RemoveSessionAsync(key);
                
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                }
                catch (Exception)
                {
                    // Ignore localStorage errors
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing state for key: {key}");
            }
        }

        public async Task ClearAllStateAsync()
        {
            try
            {
                await ClearSessionAsync();
                
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.clear");
                }
                catch (Exception)
                {
                    // Ignore localStorage errors
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing all state");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                // Check localStorage first
                try
                {
                    var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                    if (!string.IsNullOrEmpty(value))
                        return true;
                }
                catch (Exception)
                {
                    // Fall back to session
                }

                // Check session
                return await SessionExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if key exists: {key}");
                return false;
            }
        }

        // Session helper methods
        private async Task<T?> GetSessionAsync<T>(string key) where T : class
        {
            try
            {
                if (Session == null) return null;

                await Session.LoadAsync();
                var value = Session.GetString(key);
                
                if (string.IsNullOrEmpty(value))
                    return null;

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session state for key: {key}");
                return null;
            }
        }

        private async Task SetSessionAsync<T>(string key, T value) where T : class
        {
            try
            {
                if (Session == null) return;

                await Session.LoadAsync();
                var serializedValue = JsonConvert.SerializeObject(value);
                Session.SetString(key, serializedValue);
                await Session.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting session state for key: {key}");
            }
        }

        private async Task RemoveSessionAsync(string key)
        {
            try
            {
                if (Session == null) return;

                await Session.LoadAsync();
                Session.Remove(key);
                await Session.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing session state for key: {key}");
            }
        }

        private async Task ClearSessionAsync()
        {
            try
            {
                if (Session == null) return;

                await Session.LoadAsync();
                Session.Clear();
                await Session.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing session state");
            }
        }

        private async Task<bool> SessionExistsAsync(string key)
        {
            try
            {
                if (Session == null) return false;

                await Session.LoadAsync();
                return Session.Keys.Contains(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking session key exists: {key}");
                return false;
            }
        }

        // Sync methods for compatibility
        public T? GetState<T>(string key) where T : class
        {
            try
            {
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
