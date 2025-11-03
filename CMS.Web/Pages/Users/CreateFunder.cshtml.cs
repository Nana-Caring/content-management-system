using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Text.Json;

namespace CMS.Web.Pages.Users
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateFunderModel : BasePageModel
    {
        private readonly IApiService _apiService;

        [BindProperty]
        public CreateFunderRequest FunderRequest { get; set; } = new();
        
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public CreateFunderModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public void OnGet()
        {
            ViewData["Title"] = "Register New Funder";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Call backend endpoint: POST /admin/users/register-funder
                var response = await _apiService.PostAsync<CMS.Web.Models.ApiResponse<User>>("/admin/users/register-funder", FunderRequest);
                
                if (response?.Success == true)
                {
                    SuccessMessage = $"Funder '{FunderRequest.FirstName} {FunderRequest.Surname}' registered successfully!";
                    FunderRequest = new(); // Clear form
                    
                    // Return JSON for AJAX requests
                    if (Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        return new JsonResult(new { 
                            success = true, 
                            message = SuccessMessage,
                            user = response.Data 
                        });
                    }
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Failed to register funder";
                    
                    if (Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        return new JsonResult(new { 
                            success = false, 
                            message = ErrorMessage 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                
                if (Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = ErrorMessage 
                    });
                }
            }

            return Page();
        }
    }

    public class CreateFunderRequest
    {
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Idnumber { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string HomeAddressLine1 { get; set; } = "";
        public string? HomeAddressLine2 { get; set; }
        public string HomeCity { get; set; } = "";
        public string HomeProvince { get; set; } = "";
        public string HomeCode { get; set; } = "";
        public string PostalAddressLine1 { get; set; } = "";
        public string? PostalAddressLine2 { get; set; }
        public string PostalCity { get; set; } = "";
        public string PostalProvince { get; set; } = "";
        public string PostalCode { get; set; } = "";
    }
}