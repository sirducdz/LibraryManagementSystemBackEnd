namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class BorrowingRequestDto
    {
        public int Id { get; set; }
        public int RequestorId { get; set; }
        public string? RequestorName { get; set; } // Lấy từ User liên quan
        public DateTime DateRequested { get; set; }
        public string Status { get; set; } = string.Empty; // Ví dụ: "Waiting"
        public string? RejectionReason { get; set; }
        public List<BorrowingRequestDetailDto> Details { get; set; } = new List<BorrowingRequestDetailDto>();
        // Có thể thêm ApproverId, DateProcessed sau này
    }
}
