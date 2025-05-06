namespace LibraryManagement.API.Models.DTOs.QueryParameters
{
    public class UserQueryParameters : PaginationParameters
    {
        public string? SearchTerm { get; set; } // Tìm theo UserName, FullName, Email
        public int? RoleId { get; set; } // Lọc theo Role
        public bool? IsActive { get; set; } // Lọc theo trạng thái Active
        public string? SortBy { get; set; } = "UserName"; // Mặc định sort theo UserName
        public string? SortOrder { get; set; } = "asc";

    }
}
