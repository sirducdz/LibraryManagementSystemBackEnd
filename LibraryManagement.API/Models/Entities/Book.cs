using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("Books")]
    public class Book : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Author { get; set; }

        [MaxLength(20)]
        public string? ISBN { get; set; } // Nên có unique index nếu dùng

        [MaxLength(100)]
        public string? Publisher { get; set; }

        public int? PublicationYear { get; set; }

        public string? Description { get; set; }

        public string? CoverImageUrl { get; set; }

        [Required]
        public int CategoryID { get; set; } // Foreign Key

        [Required]
        [Range(0, int.MaxValue)]
        public int TotalQuantity { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(3, 2)")] // Chỉ định kiểu dữ liệu SQL
        public decimal AverageRating { get; set; } = 0.00m;

        [Required]
        [Range(0, int.MaxValue)]
        public int RatingCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false; // Soft delete flag

        // ----- Navigation Properties -----
        [ForeignKey("CategoryID")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<BookBorrowingRequestDetails> BorrowingDetails { get; set; } = new List<BookBorrowingRequestDetails>();
        public virtual ICollection<BookRating> Ratings { get; set; } = new List<BookRating>();
    }
}
