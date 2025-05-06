using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string? RoleName { get; set; } // Lấy từ Role liên quan
        public bool IsActive { get; set; }
        public Gender Gender { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
