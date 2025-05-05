namespace LibraryManagement.API.Models.DTOs.Borrowing
{
    public class BorrowingRequestDetailViewDto
    {
        public int Id { get; set; }
        public DateTime DateRequested { get; set; }
        public string Status { get; set; } = string.Empty; // Trả về dạng string (từ enum)
        public DateTime? DueDate { get; set; }
        public DateTime? DateProcessed { get; set; }
        public string? RejectionReason { get; set; }

        // Thông tin chi tiết Người yêu cầu
        public int RequestorId { get; set; }
        public string? RequestorFullName { get; set; }
        public string? RequestorEmail { get; set; } // Thêm Email nếu cần

        // Thông tin chi tiết Người duyệt (nếu có)
        public int? ApproverId { get; set; }
        public string? ApproverFullName { get; set; }
        // public string? ApproverEmail { get; set; } // Thêm nếu cần

        // Danh sách sách chi tiết hơn
        public List<BookInfoForRequestDetailDto> Details { get; set; } = new List<BookInfoForRequestDetailDto>();
    }
}
