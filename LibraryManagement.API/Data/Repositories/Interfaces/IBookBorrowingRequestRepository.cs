using LibraryManagement.API.Models.Entities;

namespace LibraryManagement.API.Data.Repositories.Interfaces
{
    public interface IBookBorrowingRequestRepository : IRepository<BookBorrowingRequest, int>
    {
        Task<bool> UserHasBorrowedBookAsync(int userId, int bookId, CancellationToken cancellationToken = default);
        Task<int> CountActiveRequestsForUserInMonthAsync(int userId, int year, int month, CancellationToken cancellationToken = default);
    }
}
