using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMS.Web.Models;

namespace CMS.Web.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
        string? GetEmailFromToken(string token);
        IEnumerable<Claim>? GetClaimsFromToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(GetJwtSecret());
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("user_id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "CMS",
                Audience = _configuration["Jwt:Audience"] ?? "CMS-Users"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(GetJwtSecret());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "CMS",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "CMS-Users",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                // Log as Debug instead of Error since external tokens will fail validation
                // This is expected behavior - fallback methods will handle external tokens
                _logger.LogDebug(ex, "Token validation failed (expected for external tokens)");
                return null;
            }
        }

        public string? GetEmailFromToken(string token)
        {
            try
            {
                // First try validating as local token
                var principal = ValidateToken(token);
                if (principal != null)
                {
                    // Try to get email from different claim types
                    var emailClaim = principal.FindFirst(ClaimTypes.Email) ?? 
                                    principal.FindFirst("email") ?? 
                                    principal.FindFirst("Email");
                    
                    return emailClaim?.Value;
                }

                // If validation fails, try decoding as external token (without signature validation)
                return GetEmailFromExternalToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract email from token");
                return null;
            }
        }

        private string? GetEmailFromExternalToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                // Extract email from claims without validating signature
                var emailClaim = jwtToken.Claims.FirstOrDefault(c => 
                    c.Type == "email" || c.Type == ClaimTypes.Email || c.Type == "Email");
                
                return emailClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decode external token");
                return null;
            }
        }

        public IEnumerable<Claim>? GetClaimsFromToken(string token)
        {
            try
            {
                // First try validating as local token
                var principal = ValidateToken(token);
                if (principal != null)
                {
                    return principal.Claims;
                }

                // If validation fails, try decoding as external token (without signature validation)
                return GetClaimsFromExternalToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claims from token");
                return null;
            }
        }

        private IEnumerable<Claim>? GetClaimsFromExternalToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                // Return claims without validating signature
                return jwtToken.Claims;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decode external token for claims");
                return null;
            }
        }

        private string GetJwtSecret()
        {
            return _configuration["Jwt:SecretKey"] ?? "your-super-secret-key-that-should-be-at-least-32-characters-long";
        }
    }
}
