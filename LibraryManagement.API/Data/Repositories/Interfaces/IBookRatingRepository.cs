using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IBookRatingRepository : IRepository<BookRating, int>
    {
        Task<BookRating?> FindByUserAndBookAsync(int userId, int bookId, CancellationToken cancellationToken = default);
        Task<List<BookRating>> GetRatingsForBookAsync(int bookId, CancellationToken cancellationToken = default);
        // Lấy tất cả rating (kèm User) để tính toán - có thể tối ưu hơn nếu chỉ lấy StarRating
        Task<List<BookRating>> GetAllRatingsForBookCalculationAsync(int bookId, CancellationToken cancellationToken = default);
    }
}
