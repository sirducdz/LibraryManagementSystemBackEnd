namespace LibraryManagement.API.Models.DTOs.QueryParameters
{
    public class CategoryQueryParameters : PaginationParameters
    {
        /// <summary>
        /// Từ khóa tìm kiếm theo tên Category.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Tên cột để sắp xếp. Ví dụ: "Name", "CreatedAt".
        /// Mặc định là "Name".
        /// </summary>
        public string? SortBy { get; set; } = "Name";

        /// <summary>
        /// Thứ tự sắp xếp: "asc" hoặc "desc". Mặc định là "asc".
        /// </summary>
        public string? SortOrder { get; set; } = "asc";
    }
}
