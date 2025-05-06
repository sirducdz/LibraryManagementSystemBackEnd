namespace LibraryManagement.API.Models.DTOs.Dashboard
{
    public class RecentRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty; // Trả về string cho dễ hiển thị
        public int BooksCount { get; set; } // Số lượng sách trong request
    }
}
