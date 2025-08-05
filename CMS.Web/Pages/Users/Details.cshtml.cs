using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Pages.Users
{
    public class DetailsModel : BasePageModel
    {
        private readonly IApiService _apiService;

        public DetailsModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public new User? User { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["Title"] = "User Details";
            
            try
            {
                User = await _apiService.GetUserByIdAsync(id);

                if (User == null)
                {
                    ErrorMessage = "User not found or unable to connect to the backend service.";
                    return Page();
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading user details: {ex.Message}";
                Console.WriteLine($"Error in DetailsModel.OnGetAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Page();
            }
        }
    }
}
