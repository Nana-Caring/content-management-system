using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;

namespace CMS.Web.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Admin login endpoint
        /// </summary>
        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Try local authentication first
                var localResult = await _authService.AuthenticateAdminLocalAsync(request);
                if (localResult.Success)
                {
                    return Ok(localResult);
                }

                // Fall back to external API authentication
                var externalResult = await _authService.AuthenticateAdminAsync(request);
                if (externalResult.Success)
                {
                    return Ok(externalResult);
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Alternative admin login endpoints for compatibility
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            return await AdminLogin(request);
        }
    }

    /// <summary>
    /// Admin routes for compatibility
    /// </summary>
    [ApiController]
    [Route("admin")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AdminAuthController> _logger;

        public AdminAuthController(IAuthService authService, ILogger<AdminAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Try local authentication first
                var localResult = await _authService.AuthenticateAdminLocalAsync(request);
                if (localResult.Success)
                {
                    return Ok(localResult);
                }

                // Fall back to external API authentication
                var externalResult = await _authService.AuthenticateAdminAsync(request);
                if (externalResult.Success)
                {
                    return Ok(externalResult);
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("auth/login")]
        public async Task<IActionResult> AdminAuthLogin([FromBody] AdminLoginRequest request)
        {
            return await AdminLogin(request);
        }
    }
}
