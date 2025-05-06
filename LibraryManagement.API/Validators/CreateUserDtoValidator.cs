using FluentValidation;
using LibraryManagement.API.Models.DTOs.User;

namespace LibraryManagement.API.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().MaximumLength(100).EmailAddress();
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
            RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password).WithMessage("Passwords do not match.");
            RuleFor(x => x.RoleID).GreaterThan(0).WithMessage("Valid RoleID is required."); // Đảm bảo RoleID > 0
            RuleFor(x => x.Gender).IsInEnum().When(x => x.Gender.HasValue);
        }
    }
}
