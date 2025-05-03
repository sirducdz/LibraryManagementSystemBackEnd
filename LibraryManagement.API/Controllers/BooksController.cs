using LibraryManagement.API.Models.DTOs.Book;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookService bookService, ILogger<BooksController> logger)
        {
            _bookService = bookService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách sách có phân trang và lọc.
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Cho phép xem công khai
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<BookSummaryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<PagedResult<BookSummaryDto>>> GetBooks(
            [FromQuery] BookQueryParameters queryParams, CancellationToken cancellationToken)
        {
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0)
            {
                return BadRequest(new { message = "Page number and page size must be greater than 0." });
            }
            try
            {
                var pagedResult = await _bookService.GetBooksAsync(queryParams, cancellationToken);
                // Thêm header phân trang
                Response.Headers.Append("X-Pagination-Page", pagedResult.Page.ToString());
                Response.Headers.Append("X-Pagination-PageSize", pagedResult.PageSize.ToString());
                Response.Headers.Append("X-Pagination-TotalItems", pagedResult.TotalItems.ToString());
                Response.Headers.Append("X-Pagination-TotalPages", pagedResult.TotalPages.ToString());
                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching books with query: {@QueryParams}", queryParams);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một cuốn sách theo ID.
        /// </summary>
        /// <param name="id">ID của sách.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpGet("{id:int}", Name = "GetBookById")]
        [AllowAnonymous] // Cho phép xem công khai
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BookDetailDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<BookDetailDto>> GetBookById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id, cancellationToken);
                if (book == null)
                {
                    return NotFound(); // Trả về 404 Not Found
                }
                return Ok(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching book with ID {BookId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Tạo một cuốn sách mới (chỉ SuperUser).
        /// </summary>
        /// <param name="bookDto">Thông tin sách mới.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [Authorize(Roles = "SuperUser")] // <<< Chỉ SuperUser được tạo sách
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(BookDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi validation hoặc logic
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto bookDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _bookService.CreateBookAsync(bookDto, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to create book." });
                }
                // Trả về 201 Created với location header và thông tin sách vừa tạo
                return CreatedAtAction(nameof(GetBookById), new { id = result.CreatedBook!.Id }, result.CreatedBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating book: {@BookDto}", bookDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Cập nhật thông tin một cuốn sách (chỉ SuperUser).
        /// </summary>
        /// <param name="id">ID của sách cần cập nhật.</param>
        /// <param name="bookDto">Thông tin cập nhật.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperUser")] // <<< Chỉ SuperUser được cập nhật
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Thành công không trả về body
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto bookDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // (Tùy chọn) Kiểm tra id có khớp trong DTO không nếu DTO có Id
            // if (id != bookDto.Id) return BadRequest(new { message = "ID mismatch."});

            try
            {
                var result = await _bookService.UpdateBookAsync(id, bookDto, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to update book." });
                }
                return NoContent(); // Trả về 204 No Content khi cập nhật thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating book ID {BookId} with data: {@BookDto}", id, bookDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Xóa mềm một cuốn sách (chỉ SuperUser).
        /// </summary>
        /// <param name="id">ID của sách cần xóa.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperUser")] // <<< Chỉ SuperUser được xóa
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> DeleteBook(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _bookService.DeleteBookAsync(id, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    // Có thể thêm kiểm tra lỗi không thể xóa do đang mượn
                    // if (result.ErrorMessage?.Contains("active borrowings") ?? false) return BadRequest(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to delete book." });
                }
                return NoContent(); // Trả về 204 No Content khi xóa thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting book ID {BookId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }
    }
}
