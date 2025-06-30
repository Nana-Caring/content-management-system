using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Pages.Transactions
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public string? ErrorMessage { get; set; }
        
        // Filter properties
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? AmountRange { get; set; }
        
        // Sort properties
        public string SortField { get; set; } = "Date";
        public string SortDirection { get; set; } = "desc";

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync(string? search, string? typeFilter, string? amountRange, 
            string? sortField, string? sortDirection)
        {
            ViewData["Title"] = "Transactions Management";
            
            // Set filter parameters
            SearchTerm = search;
            TypeFilter = typeFilter;
            AmountRange = amountRange;
            SortField = sortField ?? "Date";
            SortDirection = sortDirection ?? "desc";
            
            try
            {
                var allTransactions = await _apiService.GetTransactionsAsync();
                
                // Convert to list first to avoid expression tree issues
                var transactionsList = allTransactions.ToList();
                
                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    transactionsList = transactionsList.Where(t => 
                        (t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (t.Type != null && t.Type.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        t.Amount.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.AccountId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(typeFilter))
                {
                    transactionsList = transactionsList.Where(t => 
                        t.Type != null && t.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(amountRange))
                {
                    transactionsList = amountRange switch
                    {
                        "0-100" => transactionsList.Where(t => t.Amount >= 0 && t.Amount <= 100).ToList(),
                        "100-1000" => transactionsList.Where(t => t.Amount > 100 && t.Amount <= 1000).ToList(),
                        "1000+" => transactionsList.Where(t => t.Amount > 1000).ToList(),
                        _ => transactionsList
                    };
                }
                
                // Apply sorting
                transactionsList = SortField.ToLower() switch
                {
                    "id" => SortDirection == "asc" 
                        ? transactionsList.OrderBy(t => t.Id).ToList()
                        : transactionsList.OrderByDescending(t => t.Id).ToList(),
                    "accountid" => SortDirection == "asc" 
                        ? transactionsList.OrderBy(t => t.AccountId).ToList()
                        : transactionsList.OrderByDescending(t => t.AccountId).ToList(),
                    "type" => SortDirection == "asc" 
                        ? transactionsList.OrderBy(t => t.Type).ToList()
                        : transactionsList.OrderByDescending(t => t.Type).ToList(),
                    "amount" => SortDirection == "asc" 
                        ? transactionsList.OrderBy(t => t.Amount).ToList()
                        : transactionsList.OrderByDescending(t => t.Amount).ToList(),
                    "date" => SortDirection == "asc" 
                        ? transactionsList.OrderBy(t => t.Date).ToList()
                        : transactionsList.OrderByDescending(t => t.Date).ToList(),
                    _ => transactionsList.OrderByDescending(t => t.Date).ToList()
                };
                
                Transactions = transactionsList;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load transactions. Please try again later.";
                // Log the exception for debugging
                Console.WriteLine($"Error loading transactions: {ex.Message}");
            }
        }
        
        public string GetNextSortDirection(string field)
        {
            if (SortField?.Equals(field, StringComparison.OrdinalIgnoreCase) == true)
            {
                return SortDirection == "asc" ? "desc" : "asc";
            }
            return "asc";
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var success = await _apiService.DeleteTransactionAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Transaction deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete transaction.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the transaction.";
                // Log the exception for debugging
                Console.WriteLine($"Error deleting transaction: {ex.Message}");
            }

            return RedirectToPage();
        }
    }
}
