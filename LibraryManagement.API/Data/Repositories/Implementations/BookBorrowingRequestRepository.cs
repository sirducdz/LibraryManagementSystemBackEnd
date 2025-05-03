using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class BookBorrowingRequestRepository : Repository<BookBorrowingRequest, int>, IBookBorrowingRequestRepository
    {
        public BookBorrowingRequestRepository(LibraryDbContext context) : base(context) { }

        public async Task<bool> UserHasBorrowedBookAsync(int userId, int bookId, CancellationToken cancellationToken = default)
        {
            // Kiểm tra xem có Request nào của User đó, có chứa Book đó, và đã Approved không
            // Cần Include Details để truy cập danh sách sách trong request
            return await GetAllQueryable() // Lấy IQueryable từ lớp base
                .Include(req => req.Details) // Include danh sách chi tiết sách đã mượn
                .AnyAsync(req => req.RequestorID == userId &&
                                 req.Status == BorrowingStatus.Approved &&
                                 req.Details.Any(d => d.BookID == bookId),
                          cancellationToken);
        }
        public async Task<int> CountActiveRequestsForUserInMonthAsync(int userId, int year, int month, CancellationToken cancellationToken = default)
        {
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1); // Ngày đầu tiên của tháng sau

            return await GetAllQueryable() // Lấy IQueryable từ lớp base
                       .CountAsync(req => req.RequestorID == userId &&
                                           (req.Status == BorrowingStatus.Waiting || req.Status == BorrowingStatus.Approved) && // <<< Dùng hằng số hoặc Enum cho Status
                                           req.DateRequested >= startDate &&
                                           req.DateRequested < endDate, // So sánh ngày
                                   cancellationToken);
        }

    }
}
