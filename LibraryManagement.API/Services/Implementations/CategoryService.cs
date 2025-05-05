using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Category;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Services.Interfaces;

namespace LibraryManagement.API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;
        // private readonly IMapper _mapper; // Inject IMapper nếu dùng AutoMapper
        private readonly IBookRepository _bookRepository;

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger /*, IMapper mapper*/, IBookRepository bookRepository)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
            _bookRepository = bookRepository;
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

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching category with ID: {CategoryId}", id);
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category with ID: {CategoryId} not found.", id);
                return null;
            }
            return MapToDto(category);
            // return _mapper.Map<CategoryDto>(category); // Nếu dùng AutoMapper
        }

        public async Task<(bool Success, CategoryDto? CreatedCategory, string? ErrorMessage)> CreateCategoryAsync(CreateCategoryDto categoryDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to create category with Name: {CategoryName}", categoryDto.Name);
            try
            {
                // Kiểm tra trùng tên (không phân biệt hoa thường)
                bool nameExists = await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == categoryDto.Name.ToLower(), cancellationToken);
                if (nameExists)
                {
                    _logger.LogWarning("Create category failed: Name '{CategoryName}' already exists.", categoryDto.Name);
                    return (false, null, "Category name already exists.");
                }

                var newCategory = new Category
                {
                    Name = categoryDto.Name.Trim(),
                    Description = categoryDto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                int addedCount = await _categoryRepository.AddAsync(newCategory, cancellationToken); // Repo auto-save
                if (addedCount > 0)
                {
                    _logger.LogInformation("Successfully created category with ID: {CategoryId}", newCategory.Id);
                    return (true, MapToDto(newCategory), null);
                }
                else
                {
                    _logger.LogError("Failed to create category '{CategoryName}': AddAsync returned 0.", categoryDto.Name);
                    return (false, null, "Failed to save the new category.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category with Name: {CategoryName}", categoryDto.Name);
                return (false, null, "An unexpected error occurred while creating the category.");
            }
        }

        public async Task<(bool Success, CategoryDto? UpdatedCategory, string? ErrorMessage)> UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to update category with ID: {CategoryId}", id);
            try
            {
                var categoryToUpdate = await _categoryRepository.GetByIdAsync(id, cancellationToken);
                if (categoryToUpdate == null)
                {
                    _logger.LogWarning("Update category failed: ID {CategoryId} not found.", id);
                    return (false, null, "Category not found.");
                }

                // Kiểm tra trùng tên (loại trừ chính nó)
                bool nameExists = await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == categoryDto.Name.ToLower() && c.Id != id, cancellationToken);
                if (nameExists)
                {
                    _logger.LogWarning("Update category failed: Name '{CategoryName}' already exists for another category.", categoryDto.Name);
                    return (false, null, "Another category with this name already exists.");
                }

                // Cập nhật thuộc tính
                categoryToUpdate.Name = categoryDto.Name.Trim();
                categoryToUpdate.Description = categoryDto.Description?.Trim();
                categoryToUpdate.UpdatedAt = DateTime.UtcNow;

                int updatedCount = await _categoryRepository.UpdateAsync(categoryToUpdate, cancellationToken); // Repo auto-save
                if (updatedCount > 0)
                {
                    _logger.LogInformation("Successfully updated category with ID: {CategoryId}", id);
                    return (true, MapToDto(categoryToUpdate), null);
                }
                else
                {
                    _logger.LogWarning("Update category failed for ID {CategoryId}: UpdateAsync returned 0.", id);
                    return (false, null, "Failed to update category.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID: {CategoryId}", id);
                return (false, null, "An unexpected error occurred while updating the category.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to delete category with ID: {CategoryId}", id);
            try
            {
                var categoryToDelete = await _categoryRepository.GetByIdAsync(id, cancellationToken);
                if (categoryToDelete == null)
                {
                    _logger.LogWarning("Delete category failed: ID {CategoryId} not found.", id);
                    return (false, "Category not found.");
                }

                // <<< KIỂM TRA SÁCH LIÊN QUAN >>>
                bool hasBooks = await _bookRepository.ExistsAsync(b => b.CategoryID == id && !b.IsDeleted, cancellationToken); // Kiểm tra sách active
                if (hasBooks)
                {
                    _logger.LogWarning("Delete category failed: Category ID {CategoryId} has associated active books.", id);
                    return (false, "Cannot delete category because it has associated books.");
                }
                // -----------------------------

                // Thực hiện xóa cứng (Hard Delete vì Category không có IsDeleted)
                int deletedCount = await _categoryRepository.RemoveAsync(categoryToDelete, cancellationToken); // Repo auto-save

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Successfully deleted category with ID: {CategoryId}", id);
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("Delete category failed for ID {CategoryId}: RemoveAsync returned 0.", id);
                    return (false, "Failed to delete category.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID: {CategoryId}", id);
                return (false, "An unexpected error occurred while deleting the category.");
            }
        }

        // Hàm helper MapToDto
        private CategoryDto MapToDto(Category category)
        {
            // if (_mapper != null) return _mapper.Map<CategoryDto>(category); // Nếu dùng AutoMapper
            if (category == null) return null!; // Hoặc throw
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        // Implement các phương thức khác của ICategoryService ở đây...
    }
}
