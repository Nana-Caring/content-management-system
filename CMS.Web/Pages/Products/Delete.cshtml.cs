using System.Threading.Tasks;
using CMS.Web.Services;
using CMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Pages.Products
{
    [Authorize]
    public class DeleteModel : BasePageModel
    {
        private readonly IApiService _api;

        public DeleteModel(IAppStateManager stateManager, IApiService api) : base(stateManager)
        {
            _api = api;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await _api.DeleteAdminProductAsync(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message ?? "Product deleted successfully";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message ?? "Failed to delete product";
            }
            return RedirectToPage("./Index");
        }
    }
}
