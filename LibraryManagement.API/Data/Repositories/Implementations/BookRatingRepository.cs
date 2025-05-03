using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class BookRatingRepository : Repository<BookRating, int>, IBookRatingRepository
    {
        public BookRatingRepository(LibraryDbContext context) : base(context) { }

        public async Task<BookRating?> FindByUserAndBookAsync(int userId, int bookId, CancellationToken cancellationToken = default)
        {
            // Không dùng AsNoTracking vì có thể cần cập nhật/xóa rating này sau đó
            return await GetAllQueryable() // Dùng GetQueryable từ lớp Repository base
                       .FirstOrDefaultAsync(r => r.UserID == userId && r.BookID == bookId, cancellationToken);
        }

        public async Task<List<BookRating>> GetRatingsForBookAsync(int bookId, CancellationToken cancellationToken = default)
        {
            // Include User để lấy UserName hiển thị
            return await GetAllQueryable()
                       .Where(r => r.BookID == bookId)
                       .Include(r => r.User) // Include thông tin User
                       .OrderByDescending(r => r.RatingDate) // Sắp xếp mới nhất lên đầu
                       .ToListAsync(cancellationToken);
        }

        public async Task<List<BookRating>> GetAllRatingsForBookCalculationAsync(int bookId, CancellationToken cancellationToken = default)
        {
            // Chỉ cần lấy StarRating để tính toán, không cần Include User/Book
            // Tuy nhiên, để đơn giản, có thể lấy cả entity nếu hiệu năng không phải vấn đề lớn
            return await GetAllQueryable()
                       .Where(r => r.BookID == bookId)
                       .ToListAsync(cancellationToken);
            // Tối ưu hơn:
            // return await _dbSet.Where(r => r.BookID == bookId).Select(r => r.StarRating).ToListAsync(cancellationToken);
            // Nhưng hàm này cần trả về List<int> thay vì List<BookRating>
        }
    }
}