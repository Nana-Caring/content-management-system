using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Models;
using System.Collections.Generic;
using System.Linq;

namespace CMS.Web.Pages.Users
{
    public class IndexModel : PageModel
    {
        public List<User> Users { get; set; }

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
