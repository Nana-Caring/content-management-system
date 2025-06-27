using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CMS.Web.Services;
using CMS.Web.Models;

namespace CMS.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public void OnGet()
        {
            // Clear any existing error messages
            ErrorMessage = null;
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
                    Email = Input.Username, // Using Username input for Email
                    Password = Input.Password
                };

                var result = await _authService.AuthenticateAdminAsync(loginRequest);

                if (result.Success && result.Data != null)
                {
                    // Create claims for the authenticated user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, result.Data.Username),
                        new Claim(ClaimTypes.Email, result.Data.Email),
                        new Claim(ClaimTypes.Role, result.Data.Role),
                        new Claim("UserId", result.Data.Id),
                        new Claim("Token", result.Token ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation($"Admin user {Input.Username} successfully authenticated");

                    // Redirect to the dashboard or home page
                    return RedirectToPage("/Index");
                }
                else
                {
                    ErrorMessage = result.Message ?? "Invalid username or password.";
                    _logger.LogWarning($"Failed login attempt for email: {Input.Username}");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login attempt for email: {Input.Username}");
                ErrorMessage = "An error occurred during login. Please try again.";
                return Page();
            }
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required")]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;
        }
    }
}
