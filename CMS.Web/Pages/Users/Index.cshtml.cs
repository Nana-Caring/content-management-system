using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Web.Models;
using CMS.Web.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Pages.Users
{
    [Authorize]
    public class IndexModel : BasePageModel
    {
        private readonly IApiService _apiService;
        public List<User> Users { get; set; } = new List<User>();
        public string? ErrorMessage { get; set; }
        
        // Filter properties
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public string? RelationFilter { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        
        // Sort properties
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalUsers { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalUsers / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        // Available filter options
        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<string> AvailableRelations { get; set; } = new List<string>();

        public IndexModel(IAppStateManager stateManager, IApiService apiService) : base(stateManager)
        {
            _apiService = apiService;
        }

        public async Task OnGetAsync(string? search, string? roleFilter, string? relationFilter, 
            DateTime? createdFrom, DateTime? createdTo, string? sortBy, string? sortDirection, int? page)
        {
            ViewData["Title"] = "Users Management";
            
            // Set filter parameters
            SearchTerm = search;
            RoleFilter = roleFilter;
            RelationFilter = relationFilter;
            CreatedFromDate = createdFrom;
            CreatedToDate = createdTo;
            SortBy = sortBy ?? "CreatedAt";
            SortDirection = sortDirection ?? "desc";
            CurrentPage = page ?? 1;
            
            try
            {
                var allUsers = await _apiService.GetUsersAsync();
                
                // Populate filter options
                AvailableRoles = allUsers.Select(u => u.Role).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).ToList();
                AvailableRelations = allUsers.Select(u => u.Relation).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).Cast<string>().ToList();
                
                // Apply filters
                var filteredUsers = ApplyFilters(allUsers);
                
                // Store total count before pagination
                TotalUsers = filteredUsers.Count;
                
                // Apply sorting and pagination
                var sortedUsers = ApplySorting(filteredUsers);
                Users = ApplyPagination(sortedUsers);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load users. Please try again later.";
                // Log the exception for debugging
                Console.WriteLine($"Error loading users: {ex.Message}");
            }
        }

        private List<User> ApplyFilters(List<User> users)
        {
            var filtered = users.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(u =>
                    u.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.MiddleName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Surname.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.IdNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Relation != null && u.Relation.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                );
            }

            // Role filter
            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                filtered = filtered.Where(u => u.Role.Equals(RoleFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Relation filter
            if (!string.IsNullOrWhiteSpace(RelationFilter))
            {
                filtered = filtered.Where(u => u.Relation != null && u.Relation.Equals(RelationFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Date range filter
            if (CreatedFromDate.HasValue)
            {
                filtered = filtered.Where(u => u.CreatedAt.Date >= CreatedFromDate.Value.Date);
            }

            if (CreatedToDate.HasValue)
            {
                filtered = filtered.Where(u => u.CreatedAt.Date <= CreatedToDate.Value.Date);
            }

            return filtered.ToList();
        }

        private List<User> ApplySorting(List<User> users)
        {
            var sorted = users.AsEnumerable();

            switch (SortBy.ToLower())
            {
                case "id":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.Id) : sorted.OrderBy(u => u.Id);
                    break;
                case "firstname":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.FirstName) : sorted.OrderBy(u => u.FirstName);
                    break;
                case "surname":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.Surname) : sorted.OrderBy(u => u.Surname);
                    break;
                case "fullname":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.FullName) : sorted.OrderBy(u => u.FullName);
                    break;
                case "email":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.Email) : sorted.OrderBy(u => u.Email);
                    break;
                case "role":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.Role) : sorted.OrderBy(u => u.Role);
                    break;
                case "idnumber":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.IdNumber) : sorted.OrderBy(u => u.IdNumber);
                    break;
                case "relation":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.Relation ?? "") : sorted.OrderBy(u => u.Relation ?? "");
                    break;
                case "phonenumber":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.PhoneNumber ?? "") : sorted.OrderBy(u => u.PhoneNumber ?? "");
                    break;
                case "updatedat":
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.UpdatedAt) : sorted.OrderBy(u => u.UpdatedAt);
                    break;
                case "createdat":
                default:
                    sorted = SortDirection == "desc" ? sorted.OrderByDescending(u => u.CreatedAt) : sorted.OrderBy(u => u.CreatedAt);
                    break;
            }

            return sorted.ToList();
        }

        private List<User> ApplyPagination(List<User> users)
        {
            var skip = (CurrentPage - 1) * PageSize;
            return users.Skip(skip).Take(PageSize).ToList();
        }

        public string GetSortIcon(string columnName)
        {
            if (SortBy.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return SortDirection == "desc" ? "fas fa-sort-down" : "fas fa-sort-up";
            }
            return "fas fa-sort";
        }

        public string GetSortUrl(string columnName)
        {
            var newDirection = (SortBy.Equals(columnName, StringComparison.OrdinalIgnoreCase) && SortDirection == "asc") ? "desc" : "asc";
            
            var parameters = new List<string>();
            
            if (!string.IsNullOrEmpty(SearchTerm))
                parameters.Add($"search={Uri.EscapeDataString(SearchTerm)}");
                
            if (!string.IsNullOrEmpty(RoleFilter))
                parameters.Add($"roleFilter={Uri.EscapeDataString(RoleFilter)}");
                
            if (!string.IsNullOrEmpty(RelationFilter))
                parameters.Add($"relationFilter={Uri.EscapeDataString(RelationFilter)}");
                
            if (CreatedFromDate.HasValue)
                parameters.Add($"createdFrom={CreatedFromDate.Value:yyyy-MM-dd}");
                
            if (CreatedToDate.HasValue)
                parameters.Add($"createdTo={CreatedToDate.Value:yyyy-MM-dd}");
            
            parameters.Add($"sortBy={columnName}");
            parameters.Add($"sortDirection={newDirection}");
            
            return $"?{string.Join("&", parameters)}";
        }

        public string GetPageUrl(int page)
        {
            var parameters = new List<string>();
            
            if (!string.IsNullOrEmpty(SearchTerm))
                parameters.Add($"search={Uri.EscapeDataString(SearchTerm)}");
                
            if (!string.IsNullOrEmpty(RoleFilter))
                parameters.Add($"roleFilter={Uri.EscapeDataString(RoleFilter)}");
                
            if (!string.IsNullOrEmpty(RelationFilter))
                parameters.Add($"relationFilter={Uri.EscapeDataString(RelationFilter)}");
                
            if (CreatedFromDate.HasValue)
                parameters.Add($"createdFrom={CreatedFromDate.Value:yyyy-MM-dd}");
                
            if (CreatedToDate.HasValue)
                parameters.Add($"createdTo={CreatedToDate.Value:yyyy-MM-dd}");
            
            parameters.Add($"sortBy={SortBy}");
            parameters.Add($"sortDirection={SortDirection}");
            parameters.Add($"page={page}");
            
            return $"?{string.Join("&", parameters)}";
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var success = await _apiService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                // Log the exception for debugging
                Console.WriteLine($"Error deleting user: {ex.Message}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBlockUserAsync(int userId, [FromBody] BlockUserRequest request)
        {
            try
            {
                var result = await _apiService.BlockUserAsync(userId, request.Reason);
                
                if (result.Success)
                {
                    return new JsonResult(new { message = result.Message });
                }
                else
                {
                    Response.StatusCode = 400;
                    return new JsonResult(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { message = $"An error occurred while blocking the user: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostUnblockUserAsync(int userId)
        {
            try
            {
                var result = await _apiService.UnblockUserAsync(userId);
                
                if (result.Success)
                {
                    return new JsonResult(new { message = result.Message });
                }
                else
                {
                    Response.StatusCode = 400;
                    return new JsonResult(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { message = $"An error occurred while unblocking the user: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostSuspendUserAsync(int userId, [FromBody] BlockUserRequest request)
        {
            try
            {
                var result = await _apiService.SuspendUserAsync(userId, request.Reason);
                
                if (result.Success)
                {
                    return new JsonResult(new { message = result.Message });
                }
                else
                {
                    Response.StatusCode = 400;
                    return new JsonResult(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { message = $"An error occurred while suspending the user: {ex.Message}" });
            }
        }
    }

    public class BlockUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
