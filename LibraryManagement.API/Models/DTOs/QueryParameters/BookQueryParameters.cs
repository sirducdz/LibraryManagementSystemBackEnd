namespace LibraryManagement.API.Models.DTOs.QueryParameters
{
    public class BookQueryParameters
    {
        private const int MaxPageSize = 50; // Giới hạn kích thước trang tối đa
        private int _pageSize = 8; // Giá trị mặc định cho trang chủ

        public int Page { get; set; } = 1; // Mặc định là trang 1

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value; // Giới hạn pageSize
        }

        public int? CategoryId { get; set; } // Nullable int để lọc theo Category (tùy chọn)

        // Có thể thêm các tham số khác: sortBy, isFeatured, searchTerm...
        //public string? SortBy { get; set; }
        //public bool? IsFeatured { get; set; }
        //public string? SearchTerm { get; set; }
    }
}
