namespace LibraryManagement.API.Models.DTOs.Book
{
    public class BookSummaryDto
    {
        public int Id { get; set; } // Hoặc string nếu ID của bạn là string
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Category { get; set; } // Tên Category (lấy từ navigation property)
        public int CategoryId { get; set; } // ID Category
        public string? CoverImageUrl { get; set; }
        public decimal Rating { get; set; } // Dùng decimal cho chính xác hơn float
        public int RatingCount { get; set; }
        public bool Available { get; set; } // True nếu còn sách cho mượn

        // Các trường tùy chọn khác nếu cần
        // public string? Description { get; set; }
        public int? PublicationYear { get; set; }
        public int? Copies { get; set; }
    }
}
