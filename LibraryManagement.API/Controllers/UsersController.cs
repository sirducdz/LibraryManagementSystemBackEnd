using LibraryManagement.API.Models.DTOs.User;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize] // <<< Yêu cầu đăng nhập cho tất cả actions trong controller này
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin profile của người dùng đang đăng nhập.
        /// </summary>
        [HttpGet("me")] // Route: GET /api/users/me
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("Fetching profile for current user (ID: {UserId})", userId);
            var profile = await _userService.GetUserProfileByIdAsync(userId, cancellationToken);

            if (profile == null)
            {
                // Hiếm khi xảy ra nếu token hợp lệ, nhưng để đề phòng
                _logger.LogWarning("Profile not found for authenticated user ID: {UserId}", userId);
                return NotFound(new { message = "User profile not found." });
            }

            return Ok(profile);
        }


        /// <summary>
        /// Cập nhật thông tin profile của người dùng đang đăng nhập.
        /// </summary>
        /// <param name="profileDto">Thông tin cần cập nhật (FullName, Gender).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("me")] // Route: PUT /api/users/me
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileDto))] // Trả về profile đã cập nhật
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))] // Lỗi validation
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi logic khác
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // User không tồn tại (hiếm nếu đã authorize)
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto profileDto, CancellationToken cancellationToken)
        {
            // FluentValidation đã chạy tự động nếu ModelState không hợp lệ
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("User {UserId} attempting to update their profile.", userId);
            try
            {
                var result = await _userService.UpdateProfileAsync(userId, profileDto, cancellationToken);

                if (!result.Success)
                {
                    // Phân loại lỗi từ service
                    if (result.ErrorMessage?.Contains("not found") ?? false)
                        return NotFound(new { message = result.ErrorMessage }); // 404

                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to update profile." }); // 400
                }

                // Trả về 200 OK với thông tin profile đã cập nhật
                return Ok(result.UpdatedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating profile for user {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        // --- Các actions khác liên quan đến User (ví dụ: Admin quản lý user) có thể thêm ở đây ---
    }
}
