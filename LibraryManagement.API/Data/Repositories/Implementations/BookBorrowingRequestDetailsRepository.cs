using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Data.Repositories.Implementations
{
    public class BookBorrowingRequestDetailsRepository : Repository<BookBorrowingRequestDetails, int>, IBookBorrowingRequestDetailsRepository
    {
        public BookBorrowingRequestDetailsRepository(LibraryDbContext context) : base(context) { }

        // >>> IMPLEMENT PHƯƠNG THỨC MỚI <<<
        public async Task<int> CountActiveBorrowsForBookAsync(int bookId, CancellationToken cancellationToken = default)
        {
            // Sử dụng GetQueryable() từ lớp Repository base hoặc _dbSet trực tiếp
            // Cần Include Request để lấy Status
            return await GetAllQueryable() // Hoặc _dbSet
                       .Include(d => d.Request) // Include thông tin Request cha
                       .CountAsync(d => d.BookID == bookId &&       // Đúng sách này
                                        d.ReturnedDate == null &&    // Chưa trả
                                        d.Request != null &&         // Kiểm tra Request không null (an toàn)
                                        d.Request.Status == BorrowingStatus.Approved, // Request đã được duyệt
                                   cancellationToken);
        }

        // ... các phương thức khác nếu có ...
    }
}
