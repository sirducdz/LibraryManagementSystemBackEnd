using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasData(
                new Category
                {
                    Id = 1, // Bắt đầu ID từ 1 hoặc số tiếp theo nếu đã có dữ liệu seed khác
                    Name = "Fiction",
                    Description = "Books based on imagination rather than fact.", // Thêm mô tả nếu muốn
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Hoặc DateTime.UtcNow nếu muốn thời gian hiện tại khi migration
                },
                new Category
                {
                    Id = 2,
                    Name = "Science",
                    Description = "Books related to various scientific fields.",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 3,
                    Name = "History",
                    Description = "Books about past events.",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 4,
                    Name = "Psychology",
                    Description = "Books concerning the study of the human mind and its functions.",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 5,
                    Name = "Economics",
                    Description = "Books about the production, consumption, and transfer of wealth.",
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
                // Bạn có thể thêm các thể loại khác vào đây nếu cần
            );
        }
    }
}
