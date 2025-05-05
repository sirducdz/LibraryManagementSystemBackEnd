using FluentValidation;
using LibraryManagement.API.Models.DTOs.User;

namespace LibraryManagement.API.Validators
{
    public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
    {
        public UpdateUserProfileDtoValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

            // Chỉ validate Gender nếu nó không null (vì DTO cho phép null)
            // và đảm bảo giá trị nằm trong Enum
            RuleFor(x => x.Gender)
                .IsInEnum().When(x => x.Gender.HasValue) // Chỉ áp dụng khi Gender có giá trị
                .WithMessage("Invalid gender value.");
        }
    }
}
