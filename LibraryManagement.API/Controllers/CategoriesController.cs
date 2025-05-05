using LibraryManagement.API.Models.DTOs.Category;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")] // Route: /api/categories
    [ApiController]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách tất cả các thể loại sách.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Danh sách các thể loại.</returns>
        [HttpGet] // Xử lý GET request đến /api/categories
        // [Authorize] // <<< Bật dòng này: Yêu cầu người dùng phải đăng nhập để xem danh sách
        // [Authorize(Roles = "Admin")] // <<< Hoặc bật dòng này: Chỉ Admin mới xem được
        [AllowAnonymous] // <<< Hoặc bật dòng này: Cho phép tất cả mọi người xem (kể cả chưa đăng nhập)
                         // >>> Chọn MỘT trong ba dòng trên tùy theo yêu cầu phân quyền của bạn <<<
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Nếu dùng [Authorize]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Nếu dùng [Authorize(Roles = ...)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve all categories.");
                var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
                _logger.LogInformation("Successfully retrieved {Count} categories.", categories.Count());
                return Ok(categories); // Trả về 200 OK cùng danh sách DTOs
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving categories.");
                // Trả về lỗi 500 Internal Server Error
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("{id:int}", Name = "GetCategoryById")]
        [AllowAnonymous] // << Ai cũng có thể xem chi tiết
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<CategoryDto>> GetCategoryById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
                if (category == null)
                {
                    return NotFound(); // 404 Not Found
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred." });
            }
        }

        /// <summary>
        /// Tạo một thể loại mới (Chỉ Admin).
        /// </summary>
        /// <param name="categoryDto">Thông tin thể loại mới.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost]
        [Authorize(Roles = "Admin")] // <<< Yêu cầu quyền Admin
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CategoryDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Lỗi validation hoặc logic
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto categoryDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _categoryService.CreateCategoryAsync(categoryDto, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to create category." });
                }
                // Trả về 201 Created với route đến category vừa tạo và dữ liệu của nó
                return CreatedAtAction(nameof(GetCategoryById), new { id = result.CreatedCategory!.Id }, result.CreatedCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {@CategoryDto}", categoryDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Cập nhật một thể loại đã có (Chỉ Admin).
        /// </summary>
        /// <param name="id">ID của thể loại cần cập nhật.</param>
        /// <param name="categoryDto">Thông tin cập nhật.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")] // <<< Yêu cầu quyền Admin
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))] // Trả về category đã cập nhật
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto categoryDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _categoryService.UpdateCategoryAsync(id, categoryDto, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to update category." });
                }
                return Ok(result.UpdatedCategory); // Trả về 200 OK với dữ liệu đã cập nhật
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category ID {CategoryId}: {@CategoryDto}", id, categoryDto);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Xóa một thể loại (Chỉ Admin).
        /// </summary>
        /// <remarks>Lưu ý: Chỉ xóa được category không có sách nào liên kết.</remarks>
        /// <param name="id">ID của thể loại cần xóa.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")] // <<< Yêu cầu quyền Admin
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Thành công không trả về body
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))] // Không thể xóa do có sách
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("not found") ?? false) return NotFound(new { message = result.ErrorMessage });
                    // Lỗi không xóa được do còn sách liên kết -> BadRequest
                    if (result.ErrorMessage?.Contains("associated books") ?? false) return BadRequest(new { message = result.ErrorMessage });
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to delete category." });
                }
                return NoContent(); // Trả về 204 No Content khi xóa thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category ID {CategoryId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }
    }
}
