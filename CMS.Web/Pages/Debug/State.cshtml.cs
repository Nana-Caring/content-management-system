using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Pages.Debug
{
    [Authorize]
    public class StateModel : BasePageModel
    {
        public string StateDebugInfo { get; set; } = "";
        public DateTime LastRefresh { get; set; }

        public StateModel(IAppStateManager stateManager) : base(stateManager)
        {
        }

        public async Task OnGetAsync()
        {
            StateDebugInfo = await _stateManager.GetStateDebugInfoAsync();
            LastRefresh = DateTime.UtcNow;
            
            // Log the state for debugging
            await _stateManager.LogCurrentStateAsync("Debug Page Request");
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            StateDebugInfo = await _stateManager.GetStateDebugInfoAsync();
            LastRefresh = DateTime.UtcNow;
            
            return Page();
        }

        public async Task<IActionResult> OnPostClearStateAsync()
        {
            await _stateManager.ClearAllStateAsync();
            StateDebugInfo = await _stateManager.GetStateDebugInfoAsync();
            LastRefresh = DateTime.UtcNow;
            
            TempData["Message"] = "All application state has been cleared.";
            return Page();
        }
    }
}
