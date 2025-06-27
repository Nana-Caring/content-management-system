using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMS.Web.Pages.Transactions
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public List<Transaction> Transactions { get; set; }

        public void OnGet(string search)
        {
            // Replace with your actual data retrieval logic (e.g., from a database)
            var allTransactions = new List<Transaction>
            {
                // Example data
                // new Transaction { Id = 1, Account = new Account { AccountNumber = "12345" }, Type = "Credit", Amount = 100, Date = DateTime.UtcNow, Description = "Sample" }
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                Transactions = allTransactions
                    .Where(t =>
                        (t.Account?.AccountNumber?.Contains(search) ?? false) ||
                        (t.Description?.Contains(search) ?? false))
                    .ToList();
            }
            else
            {
                Transactions = allTransactions;
            }
        }
    }
}
