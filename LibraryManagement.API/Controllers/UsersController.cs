using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<UserDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAllUsers([FromQuery] UserQueryParameters queryParams, CancellationToken cancellationToken)
        {
            // Validation cơ bản
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0) return BadRequest(new { message = "Page/PageSize must be positive." });

            try
            {
                var pagedResult = await _userService.GetAllUsersAsync(queryParams, cancellationToken);
                // Thêm header phân trang
                Response.Headers.Append("X-Pagination-Page", pagedResult.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalItems", pagedResult.TotalItems.ToString());
                Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users with query: {@QueryParams}", queryParams);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// [SuperUser] Lấy chi tiết một người dùng theo ID.
        /// </summary>
        [HttpGet("{id:int}", Name = "GetUserByIdForAdmin")] // Đặt tên khác với GetMyProfile
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserDto>> GetUserById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id, cancellationToken);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by ID {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// [SuperUser] Tạo một người dùng mới.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi validation hoặc logic
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var result = await _userService.CreateUserAsync(userDto, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to create user." });
                }
                // Trả về 201 Created với route và dữ liệu user vừa tạo
                return CreatedAtAction(nameof(GetUserById), new { id = result.CreatedUser!.Id }, result.CreatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {@UserDto}", userDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// [SuperUser] Cập nhật thông tin người dùng (FullName, RoleID, Gender).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))] // Trả về user đã update
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto userDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var result = await _userService.UpdateUserAsync(id, userDto, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to update user." });
                }
                return Ok(result.UpdatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID {UserId}: {@UserDto}", id, userDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// [SuperUser] Cập nhật trạng thái Active/Inactive cho người dùng.
        /// </summary>
        /// <param name="id">ID của người dùng.</param>
        /// <param name="statusDto">Trạng thái mới (`IsActive`: true/false).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/status")] // Route: PUT /api/users/123/status
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto statusDto, CancellationToken cancellationToken)
        {
            // FluentValidation sẽ kiểm tra [Required] cho IsActive
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _userService.UpdateUserStatusAsync(id, statusDto.IsActive, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to update user status." });
                }
                return Ok(result.UpdatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user ID {UserId}: {@StatusDto}", id, statusDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// [SuperUser] Xóa một người dùng (Xem xét dùng Soft Delete).
        /// </summary>
        /// <param name="id">ID của người dùng cần xóa.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Không xóa được do ràng buộc
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
        {
            // Không cho phép SuperUser tự xóa chính mình? (Thêm logic check nếu cần)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == id.ToString()) return BadRequest(new { message = "Cannot delete your own account." });

            try
            {
                var result = await _userService.DeleteUserAsync(id, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    // Lỗi không xóa được do ràng buộc (vd: đang mượn sách)
                    if (result.ErrorMessage?.Contains("dependencies") ?? false)
                        return BadRequest(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to delete user." });
                }
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        // --- Các actions khác liên quan đến User (ví dụ: Admin quản lý user) có thể thêm ở đây ---
    }
}
