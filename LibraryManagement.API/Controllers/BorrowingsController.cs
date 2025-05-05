using LibraryManagement.API.Models.DTOs.Borrowing;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowingsController : ControllerBase
    {
        private readonly IBorrowingService _borrowingService;
        private readonly ILogger<BorrowingsController> _logger;

        public BorrowingsController(IBorrowingService borrowingService, ILogger<BorrowingsController> logger)
        {
            _borrowingService = borrowingService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo một yêu cầu mượn sách mới. Yêu cầu đăng nhập.
        /// </summary>
        /// <param name="requestDto">Danh sách ID các cuốn sách cần mượn.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Thông tin yêu cầu vừa tạo hoặc lỗi.</returns>
        [HttpPost]
        [Authorize] // <<< Yêu cầu đăng nhập
        // [Authorize(Roles = "NormalUser")] // <<< Có thể giới hạn chỉ NormalUser được tạo request
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BorrowingRequestDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi validation, logic
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Chưa đăng nhập
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Sách không tồn tại
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> CreateBorrowingRequest([FromBody] CreateBorrowingRequestDto requestDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("Create borrowing request failed: Could not parse User ID from token.");
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("User {UserId} is creating a borrowing request.", userId);
            var result = await _borrowingService.CreateRequestAsync(requestDto, userId, cancellationToken);

            if (!result.Success)
            {
                // Phân loại lỗi dựa trên message
                if (result.ErrorMessage?.Contains("not found") ?? false)
                    return NotFound(new { message = result.ErrorMessage });
                if (result.ErrorMessage?.Contains("limit reached") ?? false)
                    return BadRequest(new { message = result.ErrorMessage }); // 400 Bad Request
                if (result.ErrorMessage?.Contains("unavailable") ?? false)
                    return BadRequest(new { message = result.ErrorMessage }); // 400 Bad Request

                // Lỗi chung
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to create borrowing request." });
            }

            // Thành công, trả về 201 Created với thông tin request vừa tạo
            // Cần có endpoint GetById để tạo CreatedAtAction chuẩn
            // return CreatedAtAction(nameof(GetBorrowingRequestById), new { id = result.CreatedRequest!.Id }, result.CreatedRequest);
            return Ok(result.CreatedRequest); // Trả về 200 OK cũng chấp nhận được trong nhiều trường hợp
        }

        // Ví dụ: User hoặc Admin đánh dấu sách đã trả
        // [HttpPut("details/{detailId}/return")]
        // [Authorize]
        // public async Task<IActionResult> MarkBookAsReturned(int detailId, CancellationToken cancellationToken) { ... }
        // --- THÊM CÁC ACTION MỚI ---

        /// <summary>
        /// Lấy danh sách yêu cầu mượn của người dùng hiện tại (đã đăng nhập).
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize] // Yêu cầu đăng nhập
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<BorrowingRequestDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<PagedResult<BorrowingRequestDto>>> GetMyRequests(
            [FromQuery] BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0) return BadRequest(new { message = "Page/PageSize must be positive." });

            try
            {
                var pagedResult = await _borrowingService.GetMyRequestsAsync(userId, queryParams, cancellationToken);
                // Thêm header phân trang nếu cần
                Response.Headers.Append("X-Pagination-Page", pagedResult.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalItems", pagedResult.TotalItems.ToString());
                Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requests for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// [Admin] Lấy tất cả yêu cầu mượn sách (có lọc và phân trang).
        /// </summary>
        [HttpGet] // Route: GET /api/borrowing-requests
        [Authorize(Roles = "Admin")] // <<< Chỉ Admin
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<BorrowingRequestDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<PagedResult<BorrowingRequestDto>>> GetAllRequests(
            [FromQuery] BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken)
        {
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0) return BadRequest(new { message = "Page/PageSize must be positive." });
            try
            {
                var pagedResult = await _borrowingService.GetAllRequestsAsync(queryParams, cancellationToken);
                // Thêm header phân trang
                Response.Headers.Append("X-Pagination-Page", pagedResult.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalItems", pagedResult.TotalItems.ToString());
                Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all requests with query: {@QueryParams}", queryParams);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// [Admin] Duyệt một yêu cầu mượn sách.
        /// </summary>
        /// <param name="id">ID của yêu cầu cần duyệt.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("{id:int}/approve")] // Route: PUT /api/borrowing-requests/123/approve
        [Authorize(Roles = "Admin")] // <<< Chỉ Admin
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BorrowingRequestDto))] // Trả về request đã update
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> ApproveRequest(int id, CancellationToken cancellationToken)
        {
            var approverUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(approverUserIdString, out int approverUserId))
            {
                return Unauthorized(new { message = "Invalid approver token." });
            }

            _logger.LogInformation("User {ApproverUserId} attempting to approve request ID {RequestId}", approverUserId, id);
            try
            {
                var result = await _borrowingService.ApproveRequestAsync(id, approverUserId, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to approve request." });
                }
                return Ok(result.UpdatedRequest); // Trả về 200 OK với request đã được cập nhật
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request ID {RequestId} by User {ApproverUserId}", id, approverUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// [Admin] Từ chối một yêu cầu mượn sách.
        /// </summary>
        /// <param name="id">ID của yêu cầu cần từ chối.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("{id:int}/reject")] // Route: PUT /api/borrowing-requests/123/reject
        [Authorize(Roles = "Admin")] // <<< Chỉ Admin
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BorrowingRequestDto))] // Trả về request đã update
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectBorrowingRequestDto rejectDto, CancellationToken cancellationToken)
        {
            var approverUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(approverUserIdString, out int approverUserId))
            {
                return Unauthorized(new { message = "Invalid approver token." });
            }

            _logger.LogInformation("User {ApproverUserId} attempting to reject request ID {RequestId}", approverUserId, id);
            try
            {
                var result = await _borrowingService.RejectRequestAsync(id, approverUserId, rejectDto?.Reason, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to reject request." });
                }
                return Ok(result.UpdatedRequest); // Trả về 200 OK với request đã được cập nhật
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request ID {RequestId} by User {ApproverUserId}", id, approverUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }
        [HttpPut("{id:int}/cancel")] // Route: PUT /api/borrowing-requests/123/cancel
        [Authorize] // <<< Yêu cầu đăng nhập. [Authorize(Roles = "NormalUser")] nếu cần giới hạn.
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BorrowingRequestDto))] // Trả về request đã cập nhật
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Request không hợp lệ để hủy
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Chưa đăng nhập
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(object))] // Không phải chủ sở hữu
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Request không tồn tại
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> CancelMyRequest(int id, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("User {UserId} attempting to cancel borrowing request ID {RequestId}", userId, id);

            try
            {
                var result = await _borrowingService.CancelRequestAsync(id, userId, cancellationToken);

                if (!result.Success)
                {
                    // Phân loại lỗi từ Service để trả về Status Code phù hợp
                    if (result.ErrorMessage?.Contains("not found") ?? false)
                        return NotFound(new { message = result.ErrorMessage });
                    if (result.ErrorMessage?.Contains("authorized") ?? false)
                        return Forbid(); // 403 Forbidden nếu không phải chủ sở hữu
                    // Các lỗi khác (vd: không đúng trạng thái 'Waiting') -> 400 Bad Request
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to cancel request." });
                }

                // Trả về 200 OK với thông tin request đã được cập nhật trạng thái Cancelled
                return Ok(result.UpdatedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling request ID {RequestId} for User {UserId}", id, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }
        [HttpGet("{id:int}", Name = "GetBorrowingRequestById")]
        [Authorize]
        // <<< Cập nhật kiểu trả về và status code description >>>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BorrowingRequestDetailViewDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Thêm 403 nếu service xử lý quyền và trả về null/exception
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        // <<< Thay đổi kiểu trả về của Action >>>
        public async Task<ActionResult<BorrowingRequestDetailViewDto>> GetBorrowingRequestById(int id, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }
            bool isAdmin = userRoles.Contains("Admin");

            _logger.LogInformation("User {UserId} (IsAdmin: {IsAdmin}) attempting to get details for request ID {RequestId}", userId, isAdmin, id);

            try
            {
                // <<< Gọi service và nhận về DTO chi tiết >>>
                var requestDto = await _borrowingService.GetRequestByIdAsync(id, userId, isAdmin, cancellationToken);

                if (requestDto == null)
                {
                    // Service trả về null có thể do không tìm thấy hoặc không có quyền
                    // Trả về 404 là phổ biến nhất, trừ khi bạn muốn phân biệt rõ 403
                    _logger.LogWarning("Request ID {RequestId} not found or user {UserId} not authorized.", id, userId);
                    return NotFound(); // 404 Not Found
                }

                return Ok(requestDto); // 200 OK với DTO chi tiết
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching borrowing request details for ID: {RequestId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }
        [HttpPut("details/{detailId:int}/extend")] // Route: PUT /api/borrowing-requests/details/123/extend
        [Authorize] // <<< Yêu cầu đăng nhập (Thường là NormalUser)
        // [Authorize(Roles = "NormalUser")] // Có thể giới hạn rõ hơn nếu cần
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BorrowingRequestDto))] // Trả về request cha đã cập nhật
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Không đủ điều kiện gia hạn
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Chưa đăng nhập
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(object))] // Không phải chủ sở hữu
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Không tìm thấy detailId
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> ExtendDueDate(int detailId, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("User {UserId} attempting to extend due date for detail ID {DetailId}", userId, detailId);

            try
            {
                var result = await _borrowingService.ExtendDueDateAsync(detailId, userId, cancellationToken);

                if (!result.Success)
                {
                    // Phân loại lỗi từ service
                    if (result.ErrorMessage?.Contains("not found") ?? false)
                        return NotFound(new { message = result.ErrorMessage });
                    if (result.ErrorMessage?.Contains("authorized") ?? false)
                        return Forbid(); // 403 Forbidden
                    // Các lỗi khác (đã trả, đã gia hạn, không đúng status...) -> 400 Bad Request
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to extend due date." });
                }

                // Trả về 200 OK với thông tin request cha đã được cập nhật
                return Ok(result.UpdatedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending due date for detail ID {DetailId} by user {UserId}", detailId, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }
    }
}
