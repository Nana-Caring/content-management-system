using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Pages.KYC
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<KycRequest> KycRequests { get; set; } = new List<KycRequest>();
        public string? ErrorMessage { get; set; }
        
        // Filter properties
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? DocTypeFilter { get; set; }
        
        // Sort properties
        public string SortField { get; set; } = "SubmittedAt";
        public string SortDirection { get; set; } = "desc";

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync(string? search, string? statusFilter, string? docTypeFilter, 
            string? sortField, string? sortDirection)
        {
            ViewData["Title"] = "KYC Verification";
            
            // Set filter parameters
            SearchTerm = search;
            StatusFilter = statusFilter;
            DocTypeFilter = docTypeFilter;
            SortField = sortField ?? "SubmittedAt";
            SortDirection = sortDirection ?? "desc";
            
            try
            {
                var allKycRequests = await _apiService.GetKycRequestsAsync();
                
                // Convert to list first to avoid expression tree issues with null propagating operators
                var requestsList = allKycRequests.ToList();
                
                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    requestsList = requestsList.Where(k => 
                        (k.User?.FullName != null && k.User.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (k.User?.Email != null && k.User.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (k.DocumentType != null && k.DocumentType.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        k.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    requestsList = requestsList.Where(k => 
                        k.Status != null && k.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                if (!string.IsNullOrWhiteSpace(docTypeFilter))
                {
                    requestsList = requestsList.Where(k => 
                        k.DocumentType != null && k.DocumentType.ToLower().Contains(docTypeFilter.ToLower())
                    ).ToList();
                }
                
                // Apply sorting
                requestsList = SortField.ToLower() switch
                {
                    "user" => SortDirection == "asc" 
                        ? requestsList.OrderBy(k => k.User?.FullName ?? "").ToList()
                        : requestsList.OrderByDescending(k => k.User?.FullName ?? "").ToList(),
                    "documenttype" => SortDirection == "asc" 
                        ? requestsList.OrderBy(k => k.DocumentType).ToList()
                        : requestsList.OrderByDescending(k => k.DocumentType).ToList(),
                    "status" => SortDirection == "asc" 
                        ? requestsList.OrderBy(k => k.Status).ToList()
                        : requestsList.OrderByDescending(k => k.Status).ToList(),
                    "submittedat" => SortDirection == "asc" 
                        ? requestsList.OrderBy(k => k.SubmittedAt).ToList()
                        : requestsList.OrderByDescending(k => k.SubmittedAt).ToList(),
                    _ => requestsList.OrderByDescending(k => k.SubmittedAt).ToList()
                };
                
                KycRequests = requestsList;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load KYC requests. Please try again later.";
                // Log the exception for debugging
                Console.WriteLine($"Error loading KYC requests: {ex.Message}");
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

        public IActionResult OnPostApprove(int id)
        {
            // TODO: Implement KYC approval logic with API
            // Example: Call API to approve KYC request
            TempData["SuccessMessage"] = "KYC request approved successfully.";
            return RedirectToPage();
        }

        public IActionResult OnPostReject(int id)
        {
            // TODO: Implement KYC rejection logic with API
            // Example: Call API to reject KYC request
            TempData["SuccessMessage"] = "KYC request rejected successfully.";
            return RedirectToPage();
        }
    }
}