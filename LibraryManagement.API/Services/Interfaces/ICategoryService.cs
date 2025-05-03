using LibraryManagement.API.Models.DTOs.Category;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        // Thêm các phương thức khác cho Category nếu cần (GetById, Create, Update, Delete...)
    }
}
