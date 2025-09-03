using Microsoft.AspNetCore.Mvc.RazorPages;
using CMS.Web.Services;
using CMS.Web.Models;

namespace CMS.Web.Pages.Portal
{
    public class DashboardModel : PortalPageModel
    {
        public User? CurrentUser { get; private set; }

        public DashboardModel(JwtService jwtService, IAuthService authService)
            : base(jwtService, authService)
        {
        }

        public async Task OnGetAsync()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
            {
                var userEmail = _jwtService.GetEmailFromToken(token);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    CurrentUser = await _authService.GetUserByEmailAsync(userEmail);
                }
            }
        }
    }
}
