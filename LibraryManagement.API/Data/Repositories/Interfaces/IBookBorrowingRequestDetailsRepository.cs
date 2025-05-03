using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IBookBorrowingRequestDetailsRepository : IRepository<BookBorrowingRequestDetails, int>
    {
        Task<int> CountActiveBorrowsForBookAsync(int bookId, CancellationToken cancellationToken = default);
    }
}
