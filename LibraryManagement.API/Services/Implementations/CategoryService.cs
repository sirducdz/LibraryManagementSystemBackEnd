using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Category;
using LibraryManagement.API.Services.Interfaces;

namespace LibraryManagement.API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;
        // private readonly IMapper _mapper; // Inject IMapper nếu dùng AutoMapper

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger /*, IMapper mapper*/)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
            // _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching all categories from repository.");
                var categories = await _categoryRepository.GetAllAsync(cancellationToken); // Giả định GetAllAsync trả về IEnumerable<Category>

                // --- Mapping thủ công từ Entity sang DTO ---
                var categoryDtos = categories.Select(category => new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                    // Map các trường khác nếu cần
                });

                _logger.LogInformation("Successfully fetched {Count} categories.", categoryDtos.Count());
                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories.");
                // Ném lại lỗi hoặc trả về danh sách rỗng tùy theo yêu cầu xử lý lỗi
                throw;
            }
        }

        // Implement các phương thức khác của ICategoryService ở đây...
    }
}
