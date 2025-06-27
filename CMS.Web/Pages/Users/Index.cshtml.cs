using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;

namespace CMS.Web.Pages.Users
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        public List<User> Users { get; set; }

        public IndexModel(IAppStateManager stateManager) : base(stateManager)
        {
        }

        public void OnGet()
        {
            // Replace with your actual data retrieval logic (e.g., from a database)
            Users = new List<User>
            {
                // Example data
                // new User { Id = 1, FullName = "John Doe", Email = "john@example.com", PhoneNumber = "1234567890", CreatedAt = DateTime.UtcNow }
            };
        }
    }
}
