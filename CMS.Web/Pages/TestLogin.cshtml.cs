using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Services;
using CMS.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Pages
{
    public class TestLoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<TestLoginModel> _logger;

        [BindProperty]
        public TestInputModel Input { get; set; } = new();

        public string? LastResponse { get; set; }
        public string? LastStatus { get; set; }

        public TestLoginModel(IAuthService authService, ILogger<TestLoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var loginRequest = new AdminLoginRequest
                {
                    Email = Input.Username,
                    Password = Input.Password
                };

                var result = await _authService.AuthenticateAdminAsync(loginRequest);
                
                LastStatus = result.Success ? "SUCCESS" : "FAILED";
                LastResponse = $"Success: {result.Success}, Message: {result.Message}";
                
                if (result.Data != null)
                {
                    LastResponse += $", User: {result.Data.Username}, Role: {result.Data.Role}";
                }
            }
            catch (Exception ex)
            {
                LastStatus = "ERROR";
                LastResponse = $"Exception: {ex.Message}";
                _logger.LogError(ex, "Error during test login");
            }

            return Page();
        }

        public class TestInputModel
        {
            [Required]
            public string Username { get; set; } = "admin@nana.com";

            [Required]
            public string Password { get; set; } = "nanacaring2025";
        }
    }
}
