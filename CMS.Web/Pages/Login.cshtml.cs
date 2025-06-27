using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (Input.Username == "admin" && Input.Password == "password")
            {
                // TODO: Set authentication cookie/session here
                return RedirectToPage("/Index");
            }
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        public class InputModel
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Password { get; set; }
        }
    }
}
