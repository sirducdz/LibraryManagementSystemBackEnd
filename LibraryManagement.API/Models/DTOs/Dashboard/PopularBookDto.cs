namespace LibraryManagement.API.Models.DTOs.Dashboard
{
    public class PopularBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public int BorrowCount { get; set; } // Số lượt được mượn thành công
        public decimal Rating { get; set; } // Điểm trung bình
        public string? CoverImageUrl { get; set; }
    }
}
