namespace LibraryManagement.API.Models.DTOs.Book
{
    public class BookDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Category { get; set; }
        public int CategoryId { get; set; }
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string? Description { get; set; }
        public string? CoverImageUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; } // Tính toán số lượng có sẵn
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
