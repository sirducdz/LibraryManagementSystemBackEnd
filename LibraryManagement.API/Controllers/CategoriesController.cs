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
        // [Authorize(Roles = "SuperUser")] // <<< Hoặc bật dòng này: Chỉ SuperUser mới xem được
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

        // --- Thêm các actions khác cho Category ở đây (GET by ID, POST, PUT, DELETE) ---
        // Ví dụ:
        // [HttpGet("{id}")]
        // [Authorize]
        // [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        // [ProducesResponseType(StatusCodes.Status404NotFound)]
        // public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken) { ... }

        // [HttpPost]
        // [Authorize(Roles = "SuperUser")] // Chỉ SuperUser mới được tạo
        // [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CategoryDto))]
        // [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        // public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createDto, CancellationToken cancellationToken) { ... }

        // ... (Tương tự cho Update và Delete) ...
    }
}
