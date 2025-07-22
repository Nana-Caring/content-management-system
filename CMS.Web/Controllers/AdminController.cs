using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMS.Web.Data;
using CMS.Web.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CMS.Web.Controllers
{
    [ApiController]
    [Route("admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly CmsDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(CmsDbContext context, ILogger<AdminController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get all users in the system (excludes passwords)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new 
                    {
                        u.Id,
                        u.FirstName,
                        u.MiddleName,
                        u.Surname,
                        u.Email,
                        u.Role,
                        u.Status,
                        u.IsBlocked,
                        u.BlockedAt,
                        u.BlockedBy,
                        u.BlockReason,
                        u.CreatedAt,
                        u.UpdatedAt,
                        u.PhoneNumber,
                        u.IdNumber,
                        u.Relation
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all accounts in the system
        /// </summary>
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts()
        {
            try
            {
                var accounts = await _context.Accounts.ToListAsync();
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all accounts");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all transactions in the system
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = await _context.Transactions
                    .Include(t => t.Account)
                    .Select(t => new 
                    {
                        t.Id,
                        t.AccountId,
                        t.Amount,
                        t.Type,
                        t.Description,
                        t.Status,
                        t.CreatedAt,
                        Date = t.Date,
                        AccountNumber = t.Account != null ? t.Account.AccountNumber : ""
                    })
                    .ToListAsync();

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transactions");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get system statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var stats = new
                {
                    users = await _context.Users.CountAsync(),
                    accounts = await _context.Accounts.CountAsync(),
                    transactions = await _context.Transactions.CountAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system stats");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all blocked/suspended users
        /// </summary>
        [HttpGet("blocked-users")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            try
            {
                var blockedUsers = await _context.Users
                    .Where(u => u.IsBlocked)
                    .Select(u => new 
                    {
                        u.Id,
                        u.FirstName,
                        u.MiddleName,
                        u.Surname,
                        u.Email,
                        u.Role,
                        u.Status,
                        u.IsBlocked,
                        u.BlockedAt,
                        u.BlockedBy,
                        u.BlockReason
                    })
                    .ToListAsync();

                return Ok(blockedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Block a user with reason
        /// </summary>
        [HttpPut("users/{userId}/block")]
        public async Task<IActionResult> BlockUser(int userId, [FromBody] BlockUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var adminUserId = GetCurrentUserId();
                
                user.IsBlocked = true;
                user.BlockedAt = DateTime.UtcNow;
                user.BlockedBy = adminUserId;
                user.BlockReason = request.Reason;
                user.Status = "blocked";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} blocked by admin {adminUserId} with reason: {request.Reason}");

                return Ok(new 
                {
                    message = "User blocked successfully",
                    user = new 
                    {
                        user.Id,
                        user.Email,
                        user.IsBlocked,
                        user.BlockedAt,
                        user.BlockedBy,
                        user.BlockReason,
                        user.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error blocking user {userId}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Unblock a user
        /// </summary>
        [HttpPut("users/{userId}/unblock")]
        public async Task<IActionResult> UnblockUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.IsBlocked = false;
                user.BlockedAt = null;
                user.BlockedBy = null;
                user.BlockReason = null;
                user.Status = "active";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} unblocked by admin {GetCurrentUserId()}");

                return Ok(new 
                {
                    message = "User unblocked successfully",
                    user = new 
                    {
                        user.Id,
                        user.Email,
                        user.IsBlocked,
                        user.BlockedAt,
                        user.BlockedBy,
                        user.BlockReason,
                        user.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unblocking user {userId}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Suspend a user temporarily
        /// </summary>
        [HttpPut("users/{userId}/suspend")]
        public async Task<IActionResult> SuspendUser(int userId, [FromBody] BlockUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var adminUserId = GetCurrentUserId();
                
                user.IsBlocked = true;
                user.BlockedAt = DateTime.UtcNow;
                user.BlockedBy = adminUserId;
                user.BlockReason = request.Reason;
                user.Status = "suspended";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} suspended by admin {adminUserId} with reason: {request.Reason}");

                return Ok(new 
                {
                    message = "User suspended successfully",
                    user = new 
                    {
                        user.Id,
                        user.Email,
                        user.IsBlocked,
                        user.BlockedAt,
                        user.BlockedBy,
                        user.BlockReason,
                        user.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error suspending user {userId}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a user permanently
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {id} deleted by admin {GetCurrentUserId()}");

                return Ok(new { message = "User deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete an account permanently
        /// </summary>
        [HttpDelete("accounts/{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                {
                    return NotFound(new { message = "Account not found" });
                }

                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Account {id} deleted by admin {GetCurrentUserId()}");

                return Ok(new { message = "Account deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting account {id}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a transaction permanently
        /// </summary>
        [HttpDelete("transactions/{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return NotFound(new { message = "Transaction not found" });
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Transaction {id} deleted by admin {GetCurrentUserId()}");

                return Ok(new { message = "Transaction deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting transaction {id}");
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get current user ID from JWT token
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }
    }

    /// <summary>
    /// Request model for blocking/suspending users
    /// </summary>
    public class BlockUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
