using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasIndex(u => u.UserName)
                              .IsUnique();

            builder.HasIndex(u => u.Email)
                   .IsUnique();

            builder.HasOne(u => u.Role)
                   .WithMany(r => r.Users)
                   .HasForeignKey(u => u.RoleID)
                   .OnDelete(DeleteBehavior.Restrict); // Không cho xóa Role nếu có User
        }
    }
}
