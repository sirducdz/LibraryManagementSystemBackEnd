using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
    {
        public void Configure(EntityTypeBuilder<UserActivityLog> builder)
        {
            builder.ToTable("UserActivityLogs");

            // Cấu hình mối quan hệ với User
            builder.HasOne(l => l.User)
                   .WithMany(u => u.ActivityLogs)
                   .HasForeignKey(l => l.UserID)
                   .IsRequired(false) // UserID có thể NULL
                   .OnDelete(DeleteBehavior.SetNull); // Giữ lại Log khi User bị xóa, set UserID=NULL

            // Cấu hình MaxLength (nếu chưa có Data Annotation)
            builder.Property(l => l.ActionType).IsRequired().HasMaxLength(100);
            builder.Property(l => l.TargetEntityType).HasMaxLength(50);
            builder.Property(l => l.TargetEntityID).HasMaxLength(100);
            builder.Property(l => l.SourceIPAddress).HasMaxLength(50);
        }
    }
}
