namespace LibraryManagement.API.Models.DTOs.BookRating
{
    public class BookRatingDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string? BookTitle { get; set; } // Lấy từ Book liên quan
        public int UserId { get; set; }
        public string? UserFullName { get; set; } // Lấy từ User liên quan
        public int StarRating { get; set; }
        public string? Comment { get; set; }
        public DateTime RatingDate { get; set; }
    }
}
