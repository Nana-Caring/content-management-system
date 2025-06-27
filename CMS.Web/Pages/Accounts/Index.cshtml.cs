using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;

namespace CMS.Web.Pages.Accounts
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        public List<Account> Accounts { get; set; }

        public IndexModel(IAppStateManager stateManager) : base(stateManager)
        {
        }

        public void OnGet(string search)
        {
            // Replace with your actual data retrieval logic (e.g., from a database)
            var allAccounts = new List<Account>
            {
                // Example:
                // new Account { Id = 1, AccountNumber = "12345", AccountName = "Main Account", AccountType = "Savings", Balance = 1000, CreatedAt = DateTime.UtcNow }
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                Accounts = allAccounts
                    .Where(a =>
                        (a.AccountNumber?.Contains(search) ?? false) ||
                        (a.AccountName?.Contains(search) ?? false) ||
                        (a.AccountType?.Contains(search) ?? false))
                    .ToList();
            }
            else
            {
                Accounts = allAccounts;
            }
        }
    }
}
