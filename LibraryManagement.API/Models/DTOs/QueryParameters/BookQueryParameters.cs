namespace LibraryManagement.API.Models.DTOs.QueryParameters
{
    public class BookQueryParameters
    {
        private const int MaxPageSize = 50; // Giới hạn kích thước trang tối đa
        private int _pageSize = 8; // Giá trị mặc định cho trang chủ
        private string? _sortOrder = "asc";
        public int Page { get; set; } = 1; // Mặc định là trang 1

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value; // Giới hạn pageSize
        }

        public int? CategoryId { get; set; } // Nullable int để lọc theo Category (tùy chọn)
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Lọc theo trạng thái có sẵn:
        /// null = Lấy tất cả (All)
        /// true = Chỉ lấy sách có sẵn (Available)
        /// false = Chỉ lấy sách đã được mượn hết (Borrowed/Unavailable)
        /// </summary>
        public bool? IsAvailable { get; set; }

        /// <summary>
        /// Tên cột để sắp xếp. Ví dụ: "Title", "Author", "AverageRating", "CreatedAt".
        /// Mặc định là "Title".
        /// </summary>
        public string? SortBy { get; set; } = "Title"; // Đặt mặc định là Title

        /// <summary>
        /// Thứ tự sắp xếp: "asc" (tăng dần) hoặc "desc" (giảm dần).
        /// Mặc định là "asc".
        /// </summary>
        public string? SortOrder
        {
            get => _sortOrder;
            set => _sortOrder = (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) ? "desc" : "asc"; // Chuẩn hóa về 'asc' hoặc 'desc'
        }
    }
}
