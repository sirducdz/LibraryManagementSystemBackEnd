using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class UpdateUserDto
    {
        public string? FullName { get; set; }
        public int RoleID { get; set; } // Admin có thể đổi Role
        public Gender? Gender { get; set; }
    }
}
