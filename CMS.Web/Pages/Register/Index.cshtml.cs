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
            // Enforce High Court role using claims-backed app state
            var role = CurrentUser?.Role ?? string.Empty;
            if (!string.Equals(role, "highcourt", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Enforce High Court role using claims-backed app state
                var role = CurrentUser?.Role ?? string.Empty;
                if (!string.Equals(role, "highcourt", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/Index");
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var isFunder = string.Equals(RegisterRequest.UserType, "funder", StringComparison.OrdinalIgnoreCase);
                var endpoint = isFunder ? "/admin/users/register-funder" : "/admin/users/register-caregiver";

                // Build payload using backend-required property names
                var payload = new Dictionary<string, object?>
                {
                    ["firstName"] = RegisterRequest.FirstName,
                    ["middleName"] = RegisterRequest.MiddleName,
                    ["surname"] = RegisterRequest.LastName,
                    ["email"] = RegisterRequest.Email,
                    ["password"] = RegisterRequest.Password,
                    ["Idnumber"] = RegisterRequest.Idnumber,
                    ["phoneNumber"] = RegisterRequest.Phone,
                };

                // Optional home address fields
                if (!string.IsNullOrWhiteSpace(RegisterRequest.HomeAddressLine1)) payload["homeAddressLine1"] = RegisterRequest.HomeAddressLine1;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.HomeAddressLine2)) payload["homeAddressLine2"] = RegisterRequest.HomeAddressLine2;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.HomeCity)) payload["homeCity"] = RegisterRequest.HomeCity;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.HomeProvince)) payload["homeProvince"] = RegisterRequest.HomeProvince;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.HomeCode)) payload["homeCode"] = RegisterRequest.HomeCode;

                // Optional postal address fields (commonly for funders)
                if (!string.IsNullOrWhiteSpace(RegisterRequest.PostalAddressLine1)) payload["postalAddressLine1"] = RegisterRequest.PostalAddressLine1;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.PostalAddressLine2)) payload["postalAddressLine2"] = RegisterRequest.PostalAddressLine2;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.PostalCity)) payload["postalCity"] = RegisterRequest.PostalCity;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.PostalProvince)) payload["postalProvince"] = RegisterRequest.PostalProvince;
                if (!string.IsNullOrWhiteSpace(RegisterRequest.PostalCode)) payload["postalCode"] = RegisterRequest.PostalCode;

                // Call the backend with bearer token via ApiService
                var response = await _apiService.PostAsync<object>(endpoint, payload);

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

        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters")]
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID number is required")]
        [RegularExpression("^[0-9]{13}$", ErrorMessage = "ID number must be exactly 13 digits")]
        [Display(Name = "ID Number")] 
        public string Idnumber { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        // Home address (optional)
        [StringLength(200)]
        [Display(Name = "Home Address Line 1")]
        public string? HomeAddressLine1 { get; set; }

        [StringLength(200)]
        [Display(Name = "Home Address Line 2")]
        public string? HomeAddressLine2 { get; set; }

        [StringLength(50)]
        [Display(Name = "Home City")]
        public string? HomeCity { get; set; }

        [StringLength(50)]
        [Display(Name = "Home Province")]
        public string? HomeProvince { get; set; }

        [StringLength(10)]
        [Display(Name = "Home Postal Code")]
        public string? HomeCode { get; set; }

        // Postal address (optional, useful for funders)
        [StringLength(200)]
        [Display(Name = "Postal Address Line 1")]
        public string? PostalAddressLine1 { get; set; }

        [StringLength(200)]
        [Display(Name = "Postal Address Line 2")]
        public string? PostalAddressLine2 { get; set; }

        [StringLength(50)]
        [Display(Name = "Postal City")]
        public string? PostalCity { get; set; }

        [StringLength(50)]
        [Display(Name = "Postal Province")]
        public string? PostalProvince { get; set; }

        [StringLength(10)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }
    }
}