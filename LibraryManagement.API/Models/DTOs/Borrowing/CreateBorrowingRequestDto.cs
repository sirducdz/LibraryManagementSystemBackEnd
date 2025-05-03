using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class CreateBorrowingRequestDto
    {
        [MinLength(1, ErrorMessage = "Must request at least one book.")]
        // Validation số lượng tối đa 5 sẽ làm trong FluentValidation hoặc Service
        public List<int> BookIds { get; set; } = new List<int>();
    }
}
