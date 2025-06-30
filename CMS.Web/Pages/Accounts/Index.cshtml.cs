using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;

namespace CMS.Web.Pages.Accounts
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<Account> Accounts { get; set; } = new List<Account>();
        public string? ErrorMessage { get; set; }
        public string? SearchTerm { get; set; }

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync(string search)
        {
            ViewData["Title"] = "Accounts Management";
            SearchTerm = search;
            
            try
            {
                var allAccounts = await _apiService.GetAccountsAsync();
                
                if (allAccounts == null)
                {
                    ErrorMessage = "Unable to connect to the backend service. The service may be starting up or experiencing issues. Please wait a moment and refresh the page.";
                    return;
                }

                if (!allAccounts.Any())
                {
                    ErrorMessage = "No accounts were found in the system. This could be normal if no accounts have been created yet.";
                }
                
                if (!string.IsNullOrWhiteSpace(search))
                {
                    Accounts = allAccounts.Where(a => 
                        (a.AccountName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (a.AccountNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (a.AccountType != null && a.AccountType.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
                else
                {
                    Accounts = allAccounts;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "The backend service is currently unavailable. This may be due to a cold start or temporary service disruption. Please try refreshing the page in a few moments.";
                // Log the full exception for debugging
                Console.WriteLine($"Accounts loading error: {ex}");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var success = await _apiService.DeleteAccountAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Account deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete account.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the account.";
                // Log the exception for debugging
                Console.WriteLine($"Error deleting account: {ex.Message}");
            }

            return RedirectToPage();
        }
    }
}
