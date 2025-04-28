using LibraryManagement.API.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("Users")]
    public class User : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Nên có unique index

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // public string Salt { get; set; } // Nếu cần Salt

        [Required]
        public int RoleID { get; set; } // Foreign Key
        [Required] // Hoặc không nếu bạn cho phép giá trị mặc định là Unknown
        public Gender Gender { get; set; } = Gender.Unknown;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // ----- Navigation Properties -----
        [ForeignKey("RoleID")]
        public virtual Role? Role { get; set; }

        [InverseProperty("Requestor")]
        public virtual ICollection<BookBorrowingRequest> RequestedBorrowings { get; set; } = new List<BookBorrowingRequest>();

        [InverseProperty("Approver")]
        public virtual ICollection<BookBorrowingRequest> ApprovedBorrowings { get; set; } = new List<BookBorrowingRequest>();

        public virtual ICollection<BookRating> Ratings { get; set; } = new List<BookRating>();

        public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>(); // Nếu có bảng Notifications
        public virtual ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
    }
}
