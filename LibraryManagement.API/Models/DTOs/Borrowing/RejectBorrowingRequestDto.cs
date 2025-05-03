using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class RejectBorrowingRequestDto
    {
        [MaxLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }
}
