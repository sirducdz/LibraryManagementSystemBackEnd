using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.QueryParameters
{
    public class BorrowingRequestQueryParameters : PaginationParameters
    {
        public int? UserId { get; set; } // Lọc theo người yêu cầu
                                         //public BorrowingStatus? Status { get; set; } 
                                         // Lọc theo trạng thái (Waiting, Approved, Rejected)
        public List<BorrowingStatus>? Statuses { get; set; }
        public DateTime? DateFrom { get; set; } // Lọc theo ngày yêu cầu từ
        public DateTime? DateTo { get; set; } // Lọc theo ngày yêu cầu đến
        public string? SortBy { get; set; } = "DateRequested"; // Mặc định sắp xếp theo ngày yêu cầu
        public string? SortOrder { get; set; } = "desc"; // Mặc định giảm dần (mới nhất trước)
    }
}
