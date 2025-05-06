namespace LibraryManagement.API.Models.DTOs.Dashboard
{
    public class DashboardDto
    {
        public DashboardStatsDto Stats { get; set; } = new DashboardStatsDto();
        public List<RecentRequestDto> RecentRequests { get; set; } = new List<RecentRequestDto>();
        public List<PopularBookDto> PopularBooks { get; set; } = new List<PopularBookDto>();
    }
}
