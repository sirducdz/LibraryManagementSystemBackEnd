using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LibraryManagement.API.Data
{
    public class LibraryDbContext : DbContext
    {
        // DbSets tương ứng với các bảng
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<BookBorrowingRequest> BookBorrowingRequests { get; set; } = null!;
        public DbSet<BookBorrowingRequestDetails> BookBorrowingRequestDetails { get; set; } = null!;
        public DbSet<BookRating> BookRatings { get; set; } = null!;
        public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
        // public DbSet<Notification> Notifications { get; set; } = null!; // Nếu dùng
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = null!;

        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        }
    }
}
