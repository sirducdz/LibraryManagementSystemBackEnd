namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class BorrowingRequestDetailDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string? BookTitle { get; set; }
    }
}
