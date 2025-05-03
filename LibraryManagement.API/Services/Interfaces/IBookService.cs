using LibraryManagement.API.Models.DTOs.Book;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IBookService
    {
        Task<PagedResult<BookSummaryDto>> GetBooksAsync(BookQueryParameters queryParams, CancellationToken cancellationToken = default);
        // Thêm các phương thức khác cho Book nếu cần...
        Task<BookDetailDto?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default);

        // Tạo sách mới
        Task<(bool Success, BookDetailDto? CreatedBook, string? ErrorMessage)> CreateBookAsync(CreateBookDto bookDto, CancellationToken cancellationToken = default);

        // Cập nhật sách
        Task<(bool Success, string? ErrorMessage)> UpdateBookAsync(int id, UpdateBookDto bookDto, CancellationToken cancellationToken = default);

        // Xóa sách (Soft Delete)
        Task<(bool Success, string? ErrorMessage)> DeleteBookAsync(int id, CancellationToken cancellationToken = default);
    }
}
