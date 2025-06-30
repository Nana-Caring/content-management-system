using CMS.Web.Models.State;
using Newtonsoft.Json;

namespace CMS.Web.Services
{
    public interface IStateService
    {
        // Async methods
        Task<T?> GetStateAsync<T>(string key) where T : class;
        Task SetStateAsync<T>(string key, T value) where T : class;
        Task RemoveStateAsync(string key);
        Task ClearAllStateAsync();
        Task<bool> ExistsAsync(string key);
        
        // Sync methods for compatibility
        T? GetState<T>(string key) where T : class;
        void SetState<T>(string key, T value) where T : class;
        void RemoveState(string key);
        void ClearState();
        bool HasState(string key);
    }

    public class SessionStateService : IStateService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionStateService> _logger;

        public SessionStateService(IHttpContextAccessor httpContextAccessor, ILogger<SessionStateService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public async Task<T?> GetStateAsync<T>(string key) where T : class
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
                _logger.LogError(ex, $"Error getting state for key: {key}");
                return null;
            }
        }

        public async Task SetStateAsync<T>(string key, T value) where T : class
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
                _logger.LogError(ex, $"Error setting state for key: {key}");
            }
        }

        public async Task RemoveStateAsync(string key)
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
                _logger.LogError(ex, $"Error removing state for key: {key}");
            }
        }

        public async Task ClearAllStateAsync()
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
                _logger.LogError(ex, "Error clearing all state");
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (Session == null) return false;

                await Session.LoadAsync();
                return !string.IsNullOrEmpty(Session.GetString(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking existence for key: {key}");
                return false;
            }
        }

        // Synchronous methods for compatibility
        public T? GetState<T>(string key) where T : class
        {
            try
            {
                if (Session == null) return default(T);

                var value = Session.GetString(key);
                
                if (string.IsNullOrEmpty(value))
                    return default(T);

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting state for key: {key}");
                return default(T);
            }
        }

        public void SetState<T>(string key, T value) where T : class
        {
            try
            {
                if (Session == null) return;

                var serializedValue = JsonConvert.SerializeObject(value);
                Session.SetString(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting state for key: {key}");
            }
        }

        public void RemoveState(string key)
        {
            try
            {
                if (Session == null) return;

                Session.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing state for key: {key}");
            }
        }

        public void ClearState()
        {
            try
            {
                if (Session == null) return;

                Session.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all state");
            }
        }

        public bool HasState(string key)
        {
            try
            {
                if (Session == null) return false;

                return !string.IsNullOrEmpty(Session.GetString(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking existence for key: {key}");
                return false;
            }
        }
    }
}
