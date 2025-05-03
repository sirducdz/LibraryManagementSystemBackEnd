using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("BookRatings")]
    public class BookRating : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserID { get; set; } // Foreign Key

        [Required]
        public int BookID { get; set; } // Foreign Key

        [Required]
        [Range(1, 5)]
        public int StarRating { get; set; }

        public string? Comment { get; set; }

        public DateTime RatingDate { get; set; } = DateTime.UtcNow;

        // ----- Navigation Properties -----
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        [ForeignKey("BookID")]
        public virtual Book? Book { get; set; }
    }
}
