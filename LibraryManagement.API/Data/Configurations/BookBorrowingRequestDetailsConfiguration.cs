using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class BookBorrowingRequestDetailsConfiguration : IEntityTypeConfiguration<BookBorrowingRequestDetails>
    {
        public void Configure(EntityTypeBuilder<BookBorrowingRequestDetails> builder)
        {
            builder.ToTable("BookBorrowingRequestDetails");

            // Cấu hình mối quan hệ với BookBorrowingRequest
            builder.HasOne(brd => brd.Request)
                   .WithMany(br => br.Details)
                   .HasForeignKey(brd => brd.RequestID)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa Request thì xóa Details

            // Cấu hình mối quan hệ với Book
            builder.HasOne(brd => brd.Book)
                   .WithMany(b => b.BorrowingDetails)
                   .HasForeignKey(brd => brd.BookID)
                   .OnDelete(DeleteBehavior.Restrict); // Không xóa Book nếu đang trong Details
        }
    }
}
