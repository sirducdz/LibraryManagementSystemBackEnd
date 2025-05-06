using LibraryManagement.API.Models.DTOs.Dashboard;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // <<< Yêu cầu quyền Admin cho cả controller này
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// [Admin] Lấy dữ liệu tổng hợp cho trang Dashboard.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dữ liệu tổng hợp cho dashboard.</returns>
        [HttpGet] // Route: GET /api/dashboard
        // Có thể đổi route thành [HttpGet("summary")] nếu muốn tường minh hơn
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<DashboardDto>> GetDashboardData(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching dashboard data for admin.");
                var data = await _dashboardService.GetDashboardDataAsync(cancellationToken);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving dashboard data." });
            }
        }
    }
}
