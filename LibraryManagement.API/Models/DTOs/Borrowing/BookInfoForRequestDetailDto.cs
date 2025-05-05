namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class BookInfoForRequestDetailDto
    {
        public int DetailId { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? OriginalDate { get; set; }
        public bool isExtended { get; set; }
    }
}
