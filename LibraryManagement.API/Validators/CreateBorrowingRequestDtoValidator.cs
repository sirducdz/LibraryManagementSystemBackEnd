using FluentValidation;
using LibraryManagement.API.Models.DTOs.Borrowing;

namespace LibraryManagement.API.Validators
{
    public class CreateBorrowingRequestDtoValidator : AbstractValidator<CreateBorrowingRequestDto>
    {
        public CreateBorrowingRequestDtoValidator()
        {
            RuleFor(x => x.BookIds)
                .NotEmpty().WithMessage("Book list cannot be empty.")
                .Must(list => list == null || (list != null && list.Count <= 5)).WithMessage("Cannot request more than 5 books at once.")
                .Must(list => list == null || list.Distinct().Count() == list.Count).WithMessage("Book list contains duplicate IDs."); // Đảm bảo không có ID trùng lặp
        }
    }
}
