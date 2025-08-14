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
        /// Get a specific user by ID (excludes password)
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Accounts)
                    .Where(u => u.Id == id)
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
                        u.SuspendedAt,
                        u.SuspendedUntil,
                        u.SuspensionReason,
                        u.SuspendedBy,
                        u.CreatedAt,
                        u.UpdatedAt,
                        u.PhoneNumber,
                        u.IdNumber,
                        u.Relation,
                        u.PostalAddressLine1,
                        u.PostalAddressLine2,
                        u.PostalCity,
                        u.PostalProvince,
                        u.PostalCode,
                        u.HomeAddressLine1,
                        u.HomeAddressLine2,
                        u.HomeCity,
                        u.HomeProvince,
                        u.HomeCode,
                        Accounts = u.Accounts.Select(a => new {
                            a.Id,
                            a.AccountNumber,
                            a.Balance
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", id);
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
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (user.Status == "blocked")
                {
                    return BadRequest(new { success = false, message = "User is already blocked" });
                }

                var adminUserId = GetCurrentUserId();
                
                user.IsBlocked = true;
                user.BlockedAt = DateTime.UtcNow;
                user.BlockedBy = adminUserId;
                user.BlockReason = request.Reason ?? "No reason provided";
                user.Status = "blocked";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} blocked by admin {adminUserId} with reason: {request.Reason}");

                return Ok(new 
                {
                    success = true,
                    message = "User blocked successfully",
                    data = new 
                    {
                        userId = user.Id,
                        email = user.Email,
                        status = user.Status,
                        blockedAt = user.BlockedAt,
                        reason = user.BlockReason
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
        /// Suspend a user for a specific period
        /// </summary>
        [HttpPut("users/{userId}/suspend")]
        public async Task<IActionResult> SuspendUser(int userId, [FromBody] SuspendUserRequest request)
        {
            try
            {
                if (request.SuspensionDays <= 0)
                {
                    return BadRequest(new { success = false, message = "Suspension days must be a positive number" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (user.Status == "suspended")
                {
                    return BadRequest(new { success = false, message = "User is already suspended" });
                }

                var adminUserId = GetCurrentUserId();
                var suspendedUntil = DateTime.UtcNow.AddDays(request.SuspensionDays);
                
                user.Status = "suspended";
                user.SuspendedAt = DateTime.UtcNow;
                user.SuspendedUntil = suspendedUntil;
                user.SuspendedBy = adminUserId;
                user.SuspensionReason = request.Reason ?? "No reason provided";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} suspended by admin {adminUserId} with reason: {request.Reason}");

                return Ok(new 
                {
                    success = true,
                    message = "User suspended successfully",
                    data = new 
                    {
                        userId = user.Id,
                        email = user.Email,
                        status = user.Status,
                        suspendedAt = user.SuspendedAt,
                        suspendedUntil = user.SuspendedUntil,
                        suspensionDays = request.SuspensionDays,
                        reason = user.SuspensionReason
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error suspending user {userId}");
                return StatusCode(500, new { success = false, message = "Failed to suspend user", error = ex.Message });
            }
        }

        /// <summary>
        /// Unsuspend a user (lift suspension early)
        /// </summary>
        [HttpPut("users/{userId}/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (user.Status != "suspended")
                {
                    return BadRequest(new { success = false, message = "User is not suspended" });
                }

                user.Status = "active";
                user.SuspendedAt = null;
                user.SuspendedUntil = null;
                user.SuspendedBy = null;
                user.SuspensionReason = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    message = "User suspension lifted successfully",
                    data = new
                    {
                        userId = user.Id,
                        email = user.Email,
                        status = user.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error lifting user suspension {userId}");
                return StatusCode(500, new { success = false, message = "Failed to lift user suspension", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a user permanently
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id, [FromBody] DeleteUserRequest? request = null)
        {
            try
            {
                // If no request body, create default request
                if (request == null)
                {
                    request = new DeleteUserRequest { ConfirmDelete = true, DeleteData = false };
                }

                if (!request.ConfirmDelete)
                {
                    return BadRequest(new { success = false, message = "Delete confirmation required. Set confirmDelete: true to proceed." });
                }

                var user = await _context.Users
                    .Include(u => u.Accounts)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Store user data for response before deletion
                var userData = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    surname = user.Surname,
                    email = user.Email,
                    role = user.Role
                };

                // Delete related data if requested
                if (request.DeleteData)
                {
                    // Delete transactions related to user's accounts
                    var accountIds = user.Accounts.Select(a => a.Id).ToList();
                    if (accountIds.Any())
                    {
                        var transactions = await _context.Transactions
                            .Where(t => accountIds.Contains(t.AccountId))
                            .ToListAsync();
                        _context.Transactions.RemoveRange(transactions);
                    }

                    // Delete user's accounts
                    _context.Accounts.RemoveRange(user.Accounts);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {id} deleted by admin {GetCurrentUserId()}");

                return Ok(new 
                { 
                    success = true, 
                    message = "User deleted successfully",
                    data = new
                    {
                        deletedUser = userData,
                        dataDeleted = request.DeleteData,
                        deletedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, new { success = false, message = "Failed to delete user", error = ex.Message });
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
    /// Request model for blocking users
    /// </summary>
    public class BlockUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for suspending users
    /// </summary>
    public class SuspendUserRequest
    {
        public string Reason { get; set; } = string.Empty;
        public int SuspensionDays { get; set; }
    }

    /// <summary>
    /// Request model for deleting users
    /// </summary>
    public class DeleteUserRequest
    {
        public bool ConfirmDelete { get; set; }
        public bool DeleteData { get; set; } = true;
    }
}
