using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class CreateUserDto
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public int RoleID { get; set; } // SuperUser sẽ gán RoleID
        public bool? IsActive { get; set; } = true; // Mặc định là active
        public Gender? Gender { get; set; }
    }
}
