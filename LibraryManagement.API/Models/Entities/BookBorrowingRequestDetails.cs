using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("BookBorrowingRequestDetails")]
    public class BookBorrowingRequestDetails : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestID { get; set; } // Foreign Key

        [Required]
        public int BookID { get; set; }    // Foreign Key

        public DateTime? DueDate { get; set; } // Hạn trả có thể khác nhau cho từng cuốn nếu logic phức tạp
        public DateTime? OriginalDueDate { get; set; }
        public bool IsExtensionUsed { get; set; } = false;
        public DateTime? ReturnedDate { get; set; }

        // ----- Navigation Properties -----
        [ForeignKey("RequestID")]
        public virtual BookBorrowingRequest? Request { get; set; }

        [ForeignKey("BookID")]
        public virtual Book? Book { get; set; }
    }
}
