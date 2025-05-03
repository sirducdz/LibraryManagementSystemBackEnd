using LibraryManagement.API.Models.DTOs.BookRating;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers
{
    [Route("api/book-ratings")] // Route cơ bản
    [ApiController]
    [Produces("application/json")]
    public class BookRatingsController : ControllerBase
    {
        private readonly IBookRatingService _bookRatingService;
        private readonly ILogger<BookRatingsController> _logger;

        public BookRatingsController(IBookRatingService bookRatingService, ILogger<BookRatingsController> logger)
        {
            _bookRatingService = bookRatingService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize] // <<< Yêu cầu đăng nhập để đánh giá
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BookRatingDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))] // Nếu sách không tồn tại
        public async Task<IActionResult> AddRating([FromBody] CreateBookRatingDto ratingDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            // Lấy UserId từ claims của token JWT
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("Add rating failed: Could not parse User ID from token.");
                return Unauthorized(new { message = "Invalid user token." });
            }

            _logger.LogInformation("User {UserId} attempting to add rating for BookId {BookId}", userId, ratingDto.BookId);
            var result = await _bookRatingService.AddRatingAsync(ratingDto, userId, cancellationToken);

            if (!result.Success)
            {
                // Phân loại lỗi dựa trên message từ service
                if (result.ErrorMessage?.Contains("not found") ?? false)
                {
                    return NotFound(new { message = result.ErrorMessage });
                }
                if (result.ErrorMessage?.Contains("already rated") ?? false)
                {
                    return BadRequest(new { message = result.ErrorMessage }); // Hoặc Conflict (409)
                }
                // Các lỗi khác
                return BadRequest(new { message = result.ErrorMessage ?? "Failed to add rating." });
            }

            // Trả về 201 Created với thông tin rating vừa tạo
            // Cần có endpoint GetRatingById để truyền vào đây
            // return CreatedAtAction(nameof(GetRatingById), new { id = result.CreatedRating!.Id }, result.CreatedRating);
            // Hoặc trả về 200 OK nếu không có endpoint GetById
            return Ok(result.CreatedRating);
        }
        [HttpGet("book/{bookId:int}")]
        [AllowAnonymous] // Cho phép xem công khai
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<BookRatingDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<PagedResult<BookRatingDto>>> GetRatingsForBook(
        int bookId,
        [FromQuery] PaginationParameters paginationParams, // Nhận tham số phân trang
        CancellationToken cancellationToken)
        {
            if (paginationParams.Page <= 0 || paginationParams.PageSize <= 0)
            {
                return BadRequest(new { message = "Page number and page size must be greater than 0." });
            }
            try
            {
                var pagedResult = await _bookRatingService.GetRatingsForBookAsync(bookId, paginationParams, cancellationToken);
                // Thêm header phân trang
                Response.Headers.Append("X-Pagination-Page", pagedResult.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalItems", pagedResult.TotalItems.ToString());
                Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ratings for BookId {BookId}", bookId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

    }
}
