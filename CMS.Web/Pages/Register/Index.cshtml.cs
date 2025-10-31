using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CMS.Web.Pages.Register
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IApiService apiService, ILogger<IndexModel> logger, IAppStateManager stateManager) 
            : base(stateManager)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [BindProperty]
        public RegisterUserRequest RegisterRequest { get; set; } = new RegisterUserRequest();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // Check if user has High Court role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "highcourt")
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Check if user has High Court role
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "highcourt")
                {
                    return RedirectToPage("/Index");
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Create the request payload for the registration API
                var registrationPayload = new
                {
                    user_type = RegisterRequest.UserType.ToLower(),
                    first_name = RegisterRequest.FirstName,
                    last_name = RegisterRequest.LastName,
                    email = RegisterRequest.Email,
                    phone = RegisterRequest.Phone,
                    date_of_birth = RegisterRequest.DateOfBirth?.ToString("yyyy-MM-dd"),
                    address = RegisterRequest.Address,
                    city = RegisterRequest.City,
                    state = RegisterRequest.State,
                    zip_code = RegisterRequest.ZipCode
                };

                // Call the registration API
                var response = await _apiService.PostAsync<object>("api/register", registrationPayload);

                if (response != null)
                {
                    SuccessMessage = $"User registered successfully! A new {RegisterRequest.UserType} account has been created.";
                    RegisterRequest = new RegisterUserRequest(); // Reset form
                    ModelState.Clear();
                }
                else
                {
                    ErrorMessage = "Registration failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                ErrorMessage = "An error occurred during registration. Please try again.";
            }

            return Page();
        }
    }

    public class RegisterUserRequest
    {
        [Required(ErrorMessage = "User type is required")]
        [Display(Name = "User Type")]
        public string UserType { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        [Display(Name = "State")]
        public string? State { get; set; }

        [StringLength(10, ErrorMessage = "Zip code cannot exceed 10 characters")]
        [Display(Name = "Zip Code")]
        public string? ZipCode { get; set; }
    }
}