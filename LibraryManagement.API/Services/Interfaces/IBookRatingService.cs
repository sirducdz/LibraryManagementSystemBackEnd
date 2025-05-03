using LibraryManagement.API.Models.DTOs.BookRating;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IBookRatingService
    {
        //Task<BookRatingDto?> GetRatingByIdAsync(int ratingId, CancellationToken cancellationToken = default);
        //Task<IEnumerable<BookRatingDto>> GetRatingsForBookAsync(int bookId, CancellationToken cancellationToken = default);
        Task<(bool Success, BookRatingDto? CreatedRating, string? ErrorMessage)> AddRatingAsync(CreateBookRatingDto ratingDto, int userId, CancellationToken cancellationToken = default);
        Task<PagedResult<BookRatingDto>> GetRatingsForBookAsync(int bookId, PaginationParameters paginationParams, CancellationToken cancellationToken = default);
    }
}
