using FluentValidation;
using LibraryManagement.API.Models.DTOs.Auth;

namespace LibraryManagement.API.Validators
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
                .Matches("^[a-zA-Z0-9_]*$").WithMessage("Username can only contain letters, numbers, and underscores.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.")
                .EmailAddress().WithMessage("Invalid email format."); // Kiểm tra định dạng email chuẩn

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
                 // Ví dụ: Yêu cầu độ phức tạp tối thiểu (có thể dùng Regex hoặc custom validator phức tạp hơn)
                 .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                 .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                 .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                 .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
            // Kết thúc rule cho Password

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match."); // Phải khớp với Password

            // Rule cho Gender (nếu có)
            RuleFor(x => x.Gender)
               .NotNull().WithMessage("Gender is required.") // Nếu bắt buộc
               .IsInEnum().WithMessage("Invalid gender value."); // Đảm bảo giá trị nằm trong enum Gender
        }
    }
}
