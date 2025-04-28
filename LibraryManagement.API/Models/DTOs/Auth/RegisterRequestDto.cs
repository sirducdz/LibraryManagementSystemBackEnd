using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.Auth
{
    public class RegisterRequestDto
    {
        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; } // Cần để xác nhận mật khẩu

        public Gender? Gender { get; set; } // Thêm ? nếu không bắt buộc, hoặc để mặc định Unknown
    }
}
