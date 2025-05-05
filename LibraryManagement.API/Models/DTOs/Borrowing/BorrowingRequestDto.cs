namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class BorrowingRequestDto
    {
        public int Id { get; set; }
        public int RequestorId { get; set; }
        public int? ApproverId { get; set; }
        public string? RequestorName { get; set; } // Lấy từ User liên quan
        public string? ApproverName { get; set; } // Lấy từ User liên quan
        public DateTime DateRequested { get; set; }
        public DateTime? DateProcessed { get; set; }
        public string Status { get; set; } = string.Empty; // Ví dụ: "Waiting"
        public string? RejectionReason { get; set; }
        public List<BorrowingRequestDetailDto> Details { get; set; } = new List<BorrowingRequestDetailDto>();
        public DateTime? DueDate { get; set; } // Hạn trả sách (nullable)
        // Có thể thêm ApproverId, DateProcessed sau này
    }
}
