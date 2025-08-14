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
        public string SortBy { get; set; } = "id";
        public string SortDirection { get; set; } = "desc";
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 15;
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
            SortBy = sortBy ?? "id";
            SortDirection = sortDirection ?? "desc";
            CurrentPage = page ?? 1;
            
            try
            {
                // Use paginated API method
                var paginatedResponse = await _apiService.GetUsersAsync(
                    page: CurrentPage,
                    pageSize: PageSize,
                    search: SearchTerm,
                    roleFilter: RoleFilter,
                    relationFilter: RelationFilter,
                    createdFrom: CreatedFromDate,
                    createdTo: CreatedToDate,
                    sortBy: SortBy,
                    sortDirection: SortDirection
                );
                
                if (paginatedResponse?.Success == true && paginatedResponse.Users?.Any() == true)
                {
                    Users = paginatedResponse.Users;
                    TotalUsers = paginatedResponse.Pagination?.Total ?? paginatedResponse.Users.Count;
                    
                    // Populate filter options - we'll need to get all users for this or use cached values
                    // For now, let's use the paginated users to build partial filter options
                    AvailableRoles = Users
                        .Where(u => !string.IsNullOrEmpty(u.Role))
                        .Select(u => u.Role)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToList();
                        
                    AvailableRelations = Users
                        .Where(u => !string.IsNullOrEmpty(u.Relation))
                        .Select(u => u.Relation!)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToList();
                    
                    // Add common values that might not be in current page
                    if (!AvailableRoles.Contains("Admin")) AvailableRoles.Add("Admin");
                    if (!AvailableRoles.Contains("User")) AvailableRoles.Add("User");
                    if (!AvailableRoles.Contains("Dependent")) AvailableRoles.Add("Dependent");
                    if (!AvailableRoles.Contains("Funder")) AvailableRoles.Add("Funder");
                    
                    if (!AvailableRelations.Contains("Spouse")) AvailableRelations.Add("Spouse");
                    if (!AvailableRelations.Contains("Child")) AvailableRelations.Add("Child");
                    if (!AvailableRelations.Contains("Parent")) AvailableRelations.Add("Parent");
                    if (!AvailableRelations.Contains("Guardian")) AvailableRelations.Add("Guardian");
                    
                    AvailableRoles = AvailableRoles.OrderBy(r => r).ToList();
                    AvailableRelations = AvailableRelations.OrderBy(r => r).ToList();
                }
                else
                {
                    // Fallback to non-paginated method and implement client-side pagination
                    var allUsers = await _apiService.GetUsersAsync();
                    
                    if (allUsers == null || !allUsers.Any())
                    {
                        // Backend service is temporarily unavailable
                        ErrorMessage = "Backend service is temporarily unavailable (this is common with Render.com cold starts). " +
                                     "The system is trying to wake up the service. Please wait a moment and refresh the page. " +
                                     "If the issue persists, the external API service may be experiencing downtime.";
                        
                        // Return empty lists
                        Users = new List<User>();
                        AvailableRoles = new List<string> { "Admin", "User", "Dependent" };
                        AvailableRelations = new List<string> { "Spouse", "Child", "Parent", "Guardian" };
                        TotalUsers = 0;
                        return;
                    }
                    
                    // Populate filter options with null checks
                    AvailableRoles = allUsers
                        .Where(u => !string.IsNullOrEmpty(u.Role))
                        .Select(u => u.Role)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToList();
                        
                    AvailableRelations = allUsers
                        .Where(u => !string.IsNullOrEmpty(u.Relation))
                        .Select(u => u.Relation!)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToList();
                    
                    // Apply filters
                    var filteredUsers = ApplyFilters(allUsers);
                    
                    // Apply sorting
                    filteredUsers = ApplySorting(filteredUsers);
                    
                    // Store total count and apply pagination manually
                    TotalUsers = filteredUsers.Count;
                    
                    // Apply pagination manually for fallback
                    var startIndex = (CurrentPage - 1) * PageSize;
                    Users = filteredUsers.Skip(startIndex).Take(PageSize).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load users. Error: {ex.Message}";
                // Initialize empty collections to prevent further errors
                Users = new List<User>();
                AvailableRoles = new List<string>();
                AvailableRelations = new List<string>();
                TotalUsers = 0;
                
                // Log the exception for debugging
                Console.WriteLine($"Error loading users: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private List<User> ApplyFilters(List<User> users)
        {
            if (users == null || !users.Any())
                return new List<User>();
                
            var filtered = users.AsEnumerable();

            // Search filter with null checks
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(u =>
                    (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.MiddleName) && u.MiddleName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.Surname) && u.Surname.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.IdNumber) && u.IdNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.Relation) && u.Relation.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }

            // Role filter with null check
            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                filtered = filtered.Where(u => !string.IsNullOrEmpty(u.Role) && u.Role.Equals(RoleFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Relation filter with null check
            if (!string.IsNullOrWhiteSpace(RelationFilter))
            {
                filtered = filtered.Where(u => !string.IsNullOrEmpty(u.Relation) && u.Relation.Equals(RelationFilter, StringComparison.OrdinalIgnoreCase));
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
