namespace LibraryManagement.API.Models.DTOs.Category
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } // Description có thể là nullable
        // Bạn có thể thêm các trường khác nếu cần thiết cho frontend
        // public DateTime CreatedAt { get; set; }
    }
}
