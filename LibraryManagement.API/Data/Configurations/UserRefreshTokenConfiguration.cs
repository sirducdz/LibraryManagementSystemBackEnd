using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.API.Data.Configurations
{
    public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            builder.HasIndex(rt => rt.Token).IsUnique();

            // Index cho UserId để tìm các token của một user
            builder.HasIndex(rt => rt.UserId);

            // Thiết lập quan hệ một-nhiều: Một User có thể có nhiều RefreshToken
            builder.HasOne(rt => rt.User)
                  .WithMany() // Không cần navigation property ngược lại trong User nếu không muốn
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}