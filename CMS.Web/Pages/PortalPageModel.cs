using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Services;
using CMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CMS.Web.Pages
{
    public class PortalPageModel : PageModel
    {
        protected readonly JwtService _jwtService;
        protected readonly IAuthService _authService;

        public PortalPageModel(JwtService jwtService, IAuthService authService)
        {
            _jwtService = jwtService;
            _authService = authService;
        }

        private string GetAuthToken()
        {
            // First try to get token from Authorization header
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length);
            }

            // If not in header, try cookie
            return HttpContext.Request.Cookies["auth_token"] ?? string.Empty;
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var token = GetAuthToken();
            System.Diagnostics.Debug.WriteLine($"PortalPageModel - Auth Token: {!string.IsNullOrEmpty(token)}");
            
            if (!string.IsNullOrEmpty(token))
            {
                var userEmail = _jwtService.GetEmailFromToken(token);
                System.Diagnostics.Debug.WriteLine($"PortalPageModel - User Email from Token: {userEmail}");
                
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var user = await _authService.GetUserByEmailAsync(userEmail);
                    System.Diagnostics.Debug.WriteLine($"PortalPageModel - User Retrieved: {user != null}");
                    System.Diagnostics.Debug.WriteLine($"PortalPageModel - User Role: {user?.Role}");
                    
                    if (user != null)
                    {
                        // Set the current user in ViewData for the layout to use
                        ViewData["CurrentUser"] = user;
                        ViewData["UserRole"] = user.Role; // Add explicit role in ViewData
                        ViewData["AuthToken"] = token;
                    }
                    else
                    {
                        // Redirect to login if user not found
                        context.Result = new RedirectToPageResult("/Login");
                        return;
                    }
                }
                else
                {
                    // Redirect to login if no email in token
                    context.Result = new RedirectToPageResult("/Login");
                    return;
                }
            }

            await next();
        }

        public IActionResult OnGet()
        {
            // Set the layout for all portal pages
            ViewData["Layout"] = "_PortalLayout";
            return Page();
        }
    }
}
