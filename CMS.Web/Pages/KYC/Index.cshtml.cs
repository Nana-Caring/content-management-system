using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System;

namespace CMS.Web.Pages.KYC
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        public List<KycRequest> KycRequests { get; set; }

        public IndexModel(IAppStateManager stateManager) : base(stateManager)
        {
        }

        public void OnGet()
        {
            // Replace with actual data retrieval logic
            KycRequests = new List<KycRequest>
            {
                new KycRequest
                {
                    Id = 1,
                    User = new User { FullName = "Jane Doe", Email = "jane@example.com" },
                    DocumentType = "Passport",
                    DocumentUrl = "/uploads/kyc/passport_jane.pdf",
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1)
                },
                new KycRequest
                {
                    Id = 2,
                    User = new User { FullName = "John Smith", Email = "john@example.com" },
                    DocumentType = "Driver's License",
                    DocumentUrl = "/uploads/kyc/license_john.pdf",
                    Status = "Approved",
                    SubmittedAt = DateTime.UtcNow.AddDays(-2)
                }
            };
        }

        public IActionResult OnPostApprove(int id)
        {
            // TODO: Update KYC status in your data source
            // Example: Set status to "Approved" for the given id
            return RedirectToPage();
        }

        public IActionResult OnPostReject(int id)
        {
            // TODO: Update KYC status in your data source
            // Example: Set status to "Rejected" for the given id
            return RedirectToPage();
        }
    }
}