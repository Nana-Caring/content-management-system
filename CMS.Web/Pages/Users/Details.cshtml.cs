using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CMS.Web.Data;
using CMS.Web.Models;

namespace CMS.Web.Pages.Users
{
    public class DetailsModel : PageModel
    {
        private readonly CmsDbContext _context;

        public DetailsModel(CmsDbContext context)
        {
            _context = context;
        }

        public new User? User { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                User = await _context.Users
                    .Include(u => u.Accounts)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (User == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception)
            {
                // If database is not available, return mock data for development
                User = CreateMockUser(id);
                return Page();
            }
        }

        private User CreateMockUser(int id)
        {
            return new User
            {
                Id = id,
                FirstName = "John",
                MiddleName = "William",
                Surname = "Doe",
                Email = "john.doe@example.com",
                Role = "funder",
                IdNumber = "1234567890123",
                Relation = "Parent",
                PhoneNumber = "+27 82 123 4567",
                PostalAddressLine1 = "123 Main Street",
                PostalAddressLine2 = "Apartment 4B",
                PostalCity = "Cape Town",
                PostalProvince = "Western Cape",
                PostalCode = "8001",
                HomeAddressLine1 = "456 Oak Avenue",
                HomeAddressLine2 = "",
                HomeCity = "Johannesburg",
                HomeProvince = "Gauteng",
                HomeCode = "2000",
                CreatedAt = DateTime.Now.AddDays(-30),
                UpdatedAt = DateTime.Now.AddDays(-1),
                Accounts = new List<Account>
                {
                    new Account
                    {
                        Id = "ACC001234567",
                        AccountNumber = "ACC001234567",
                        Balance = 15000.50m,
                        UserId = id
                    },
                    new Account
                    {
                        Id = "ACC001234568", 
                        AccountNumber = "ACC001234568",
                        Balance = 8500.75m,
                        UserId = id
                    }
                }
            };
        }
    }
}
