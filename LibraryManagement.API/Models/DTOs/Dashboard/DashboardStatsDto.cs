namespace LibraryManagement.API.Models.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalBooks { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveRequests { get; set; } // Số request đang chờ duyệt?
        public int OverdueBooks { get; set; } // Số sách đang mượn bị quá hạn
        public int BooksThisMonth { get; set; }
        public int UsersThisMonth { get; set; }
        public int RequestsThisMonth { get; set; }
        public int ReturnedThisMonth { get; set; }
    }
}
