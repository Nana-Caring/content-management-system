using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;

namespace CMS.Web.Pages.Portal
{
// [Authorize] removed for admin portal
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<Account> Accounts { get; set; } = new List<Account>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public new User? User { get; set; }  // Using 'new' keyword to hide inherited User property
        public bool IsLoggedIn { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public string? SearchTerm { get; set; }

        // Properties for Portal UI
        public User? UserInfo => User;
        public int AccountCount => Accounts?.Count ?? 0;
        public int ActiveAccounts => Accounts?.Count(a => a.Status == "active") ?? 0;
        public int TransactionCount => Transactions?.Count ?? 0;
        public int PendingTransactions => Transactions?.Count(t => t.Status == "Pending") ?? 0;

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync()
        {
            // Check if admin is logged in
            if (HttpContext.Session.TryGetValue("AdminLoggedIn", out var _))
            {
                IsLoggedIn = true;
                // Load dashboard data
                var users = await _apiService.GetUsersAsync();
                User = users.FirstOrDefault(); // For demo, show first user
                Accounts = User?.Accounts?.ToList() ?? new List<Account>();
                Transactions = await _apiService.GetTransactionsAsync();
            }
            else
            {
                IsLoggedIn = false;
            }
        }

        public async Task<IActionResult> OnPostAsync(string email, string password, string signout)
        {
            if (!string.IsNullOrEmpty(signout))
            {
                // Sign out logic
                HttpContext.Session.Remove("AdminLoggedIn");
                IsLoggedIn = false;
                User = null;
                Accounts = new List<Account>();
                Transactions = new List<Transaction>();
                return RedirectToPage();
            }

            // Simple admin login logic (replace with real auth in production)
            if (email == "admin@nanacms.com" && password == "adminpass")
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                IsLoggedIn = true;
                var users = await _apiService.GetUsersAsync();
                User = users.FirstOrDefault(); // For demo, show first user
                Accounts = User?.Accounts?.ToList() ?? new List<Account>();
                Transactions = await _apiService.GetTransactionsAsync();
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = "Invalid admin credentials.";
                IsLoggedIn = false;
                return Page();
            }
        }
    }
}
