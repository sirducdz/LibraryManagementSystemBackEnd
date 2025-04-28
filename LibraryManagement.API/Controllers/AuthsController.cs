using LibraryManagement.API.Models.DTOs.Auth;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")] // Chỉ định kiểu content trả về mặc định
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký một tài khoản người dùng mới.
        /// </summary>
        /// <param name="registerDto">Thông tin đăng ký.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Thông báo thành công hoặc lỗi.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // Hoặc 201 Created
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))] // Lỗi validation
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi logic (vd: email tồn tại)
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto, CancellationToken cancellationToken)
        {
            // ModelState.IsValid được [ApiController] kiểm tra tự động, nhưng có thể check lại nếu muốn
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState); // Trả về lỗi 400 chuẩn với chi tiết validation
            }

            _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);
            // *** Giả định RegisterAsync đã được implement đúng trong AuthService ***
            var result = await _authService.RegisterAsync(registerDto, cancellationToken);

            if (!result.Success)
            {
                // Trả về lỗi 400 Bad Request với thông báo từ service
                return BadRequest(new { message = result.ErrorMessage ?? "Registration failed." });
            }

            // Thành công: Trả về 200 OK hoặc 201 Created
            return Ok(new { message = "Registration successful", userId = result.UserId });
            // Nếu có endpoint lấy thông tin user:
            // return CreatedAtAction(nameof(UsersController.GetUserById), "Users", new { id = result.UserId }, new { message = "Registration successful" });
        }

        /// <summary>
        /// Đăng nhập và nhận về access token và refresh token.
        /// </summary>
        /// <param name="loginRequest">Thông tin đăng nhập (username/email và password).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Bộ token nếu thành công, lỗi nếu thất bại.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(object))]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            _logger.LogInformation("Login attempt for user: {UserName}", loginRequest.UserName);
            // *** Giả định LoginAsync đã được implement đúng trong AuthService ***
            var result = await _authService.LoginAsync(loginRequest, cancellationToken);

            if (!result.Success)
            {
                // Trả về 401 Unauthorized nếu sai thông tin đăng nhập
                return Unauthorized(new { message = result.ErrorMessage ?? "Login failed." });
            }

            // Trả về 200 OK với bộ tokens
            return Ok(result.Tokens);
        }

        /// <summary>
        /// Sử dụng refresh token để lấy access token mới (và refresh token mới nếu xoay vòng).
        /// </summary>
        /// <param name="tokenRequest">Yêu cầu chứa refresh token (và access token cũ nếu service cần).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Bộ token mới nếu thành công, lỗi nếu thất bại.</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(object))]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequest, CancellationToken cancellationToken) // Dùng TokenRequestDto như trong service
        {
            if (!ModelState.IsValid) // Kiểm tra DTO hợp lệ
            {
                return ValidationProblem(ModelState);
            }
            if (string.IsNullOrEmpty(tokenRequest.RefreshToken)) // Kiểm tra cụ thể RefreshToken
            {
                return BadRequest(new { message = "Refresh token is required." });
            }


            _logger.LogInformation("Attempting to refresh token.");
            // *** Giả định RefreshTokenAsync đã được implement đúng trong AuthService ***
            var result = await _authService.RefreshTokenAsync(tokenRequest, cancellationToken);

            if (!result.Success)
            {
                // Trả về 401 Unauthorized nếu refresh token không hợp lệ/hết hạn
                return Unauthorized(new { message = result.ErrorMessage ?? "Token refresh failed." });
            }

            // Trả về 200 OK với bộ tokens mới
            return Ok(result.Tokens);
        }

        /// <summary>
        /// Đăng xuất bằng cách thu hồi refresh token hiện tại (hoặc tất cả).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Không có nội dung nếu thành công, lỗi nếu thất bại.</returns>
        [HttpPost("logout")]
        [Authorize] // Yêu cầu xác thực
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Thành công không cần trả về gì
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Nếu token không hợp lệ
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Logout attempt failed: Could not extract User ID from token.");
                return Unauthorized(new { message = "Invalid token." }); // Hoặc BadRequest tùy logic
            }

            _logger.LogInformation("Logout attempt for user ID: {UserId}", userId);
            // *** Giả định LogoutAsync đã được implement đúng trong AuthService ***
            var success = await _authService.LogoutAsync(userId, cancellationToken);

            if (!success)
            {
                // Lỗi đã được log trong service
                return BadRequest(new { message = "Logout failed." });
            }

            // Trả về 204 No Content khi thành công và không có gì để trả về
            return NoContent();
        }

        /// <summary>
        /// Lấy thông tin cơ bản của người dùng đang đăng nhập.
        /// </summary>
        /// <returns>Thông tin người dùng.</returns>
        [HttpGet("me")]
        [Authorize] // Yêu cầu xác thực
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))] // << Nên thay bằng UserInfoDto cụ thể
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetMyInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email); // Hoặc JwtRegisteredClaimNames.Email
            var fullName = User.FindFirstValue(ClaimTypes.Name);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            if (string.IsNullOrEmpty(userId)) // Kiểm tra lại lần nữa cho chắc
            {
                return Unauthorized();
            }

            // Nên tạo một UserInfoDto để trả về thay vì anonymous object
            var userInfo = new
            {
                Id = userId,
                Email = userEmail,
                FullName = fullName,
                Roles = roles
            };

            return Ok(userInfo);
        }
    }
}
