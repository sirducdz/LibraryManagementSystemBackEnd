using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class BookRatingConfiguration : IEntityTypeConfiguration<BookRating>
    {
        public void Configure(EntityTypeBuilder<BookRating> builder)
        {
            builder.ToTable("BookRatings");

            // Cấu hình Composite Unique Key (UserID, BookID)
            builder.HasIndex(r => new { r.UserID, r.BookID })
                   .IsUnique();

            // Cấu hình mối quan hệ với User
            builder.HasOne(r => r.User)
                   .WithMany(u => u.Ratings)
                   .HasForeignKey(r => r.UserID)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa User thì xóa Rating

            // Cấu hình mối quan hệ với Book
            builder.HasOne(r => r.Book)
                   .WithMany(b => b.Ratings)
                   .HasForeignKey(r => r.BookID)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Book thì xóa Rating
        }
    }
}
