using LibraryManagement.API.Models.DTOs.Category;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        // Thêm các phương thức khác cho Category nếu cần (GetById, Create, Update, Delete...)
        Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<(bool Success, CategoryDto? CreatedCategory, string? ErrorMessage)> CreateCategoryAsync(CreateCategoryDto categoryDto, CancellationToken cancellationToken = default);
        Task<(bool Success, CategoryDto? UpdatedCategory, string? ErrorMessage)> UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto, CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    }
}
