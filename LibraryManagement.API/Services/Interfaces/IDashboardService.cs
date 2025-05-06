using LibraryManagement.API.Models.DTOs.Dashboard;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default);
    }
}
