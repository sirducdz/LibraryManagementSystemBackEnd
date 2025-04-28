using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class BookBorrowingRequestConfiguration : IEntityTypeConfiguration<BookBorrowingRequest>
    {
        public void Configure(EntityTypeBuilder<BookBorrowingRequest> builder)
        {
            builder.ToTable("BookBorrowingRequests");

            // Cấu hình mối quan hệ với User (Requestor)
            builder.HasOne(br => br.Requestor)
                   .WithMany(u => u.RequestedBorrowings)
                   .HasForeignKey(br => br.RequestorID)
                   .OnDelete(DeleteBehavior.NoAction); // Không làm gì khi User bị xóa (hoặc Restrict)

            // Cấu hình mối quan hệ với User (Approver)
            builder.HasOne(br => br.Approver)
                   .WithMany(u => u.ApprovedBorrowings)
                   .HasForeignKey(br => br.ApproverID)
                   .IsRequired(false) // Đảm bảo FK có thể là NULL
                   .OnDelete(DeleteBehavior.SetNull); // Set ApproverID=NULL nếu User Approver bị xóa

            // Nếu bạn dùng kiểu Enum trực tiếp cho Status thay vì string:
            builder.Property(e => e.Status)
                   .HasConversion<string>() // Lưu enum dưới dạng string vào DB
                   .HasMaxLength(20);
        }
    }
}