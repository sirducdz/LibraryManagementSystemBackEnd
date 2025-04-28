using LibraryManagement.API.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("BookBorrowingRequests")]
    public class BookBorrowingRequest : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestorID { get; set; } // Foreign Key

        public DateTime DateRequested { get; set; } = DateTime.Now;

        [Required] // Giữ lại nếu trường này là bắt buộc
        public BorrowingStatus Status { get; set; } = BorrowingStatus.Waiting;

        public int? ApproverID { get; set; } // Foreign Key, nullable

        public DateTime? DateProcessed { get; set; }
        public DateTime? DueDate { get; set; } // Hạn trả chung cho request

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ----- Navigation Properties -----
        [ForeignKey("RequestorID")]
        public virtual User? Requestor { get; set; }

        [ForeignKey("ApproverID")]
        public virtual User? Approver { get; set; }

        public virtual ICollection<BookBorrowingRequestDetails> Details { get; set; } = new List<BookBorrowingRequestDetails>();
    }
}
