using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("Books");

            // Index Unique cho ISBN (chỉ áp dụng khi ISBN không NULL)
            builder.HasIndex(b => b.ISBN)
                   .IsUnique()
                   .HasFilter("[ISBN] IS NOT NULL"); // Cú pháp SQL Server, điều chỉnh nếu dùng DB khác

            // Chỉ định kiểu dữ liệu và độ chính xác cho AverageRating
            builder.Property(b => b.AverageRating)
                   .HasColumnType("decimal(3, 2)"); // Ví dụ: 3 chữ số tổng, 2 chữ số sau dấu phẩy

            // Cấu hình Soft Delete Filter: Mặc định chỉ query các sách chưa bị xóa
            builder.HasQueryFilter(b => !b.IsDeleted);

            // Cấu hình mối quan hệ với Category (chủ yếu để set OnDelete)
            builder.HasOne(b => b.Category)
                   .WithMany(c => c.Books)
                   .HasForeignKey(b => b.CategoryID)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Category nếu còn Book
        }
    }
}
