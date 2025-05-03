using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.BookRating;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Services.Implementations
{
    public class BookRatingService : IBookRatingService
    {
        private readonly IBookRatingRepository _bookRatingRepository;
        private readonly IBookRepository _bookRepository; // Inject repo sách
        private readonly ILogger<BookRatingService> _logger;
        // private readonly LibraryDbContext _context; // Inject DbContext nếu cần transaction

        public BookRatingService(
            IBookRatingRepository bookRatingRepository,
            IBookRepository bookRepository,
            ILogger<BookRatingService> logger)
        {
            _bookRatingRepository = bookRatingRepository;
            _bookRepository = bookRepository;
            _logger = logger;
        }

        public async Task<(bool Success, BookRatingDto? CreatedRating, string? ErrorMessage)> AddRatingAsync(CreateBookRatingDto ratingDto, int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to add rating for BookId {BookId} by UserId {UserId}", ratingDto.BookId, userId);

            var bookExists = await _bookRepository.ExistsAsync(b => b.Id == ratingDto.BookId && !b.IsDeleted, cancellationToken);
            if (!bookExists)
            {
                _logger.LogWarning("Add rating failed: Book with Id {BookId} not found.", ratingDto.BookId);
                return (false, null, "Book not found.");
            }

            var existingRating = await _bookRatingRepository.FindByUserAndBookAsync(userId, ratingDto.BookId, cancellationToken);
            if (existingRating != null)
            {
                _logger.LogWarning("Add rating failed: User {UserId} already rated Book {BookId}.", userId, ratingDto.BookId);
                return (false, null, "You have already rated this book.");
            }

            var newRating = new BookRating
            {
                BookID = ratingDto.BookId,
                UserID = userId,
                StarRating = ratingDto.StarRating,
                Comment = ratingDto.Comment,
                RatingDate = DateTime.UtcNow
            };

            // using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken); // Bắt đầu transaction nếu cần
            try
            {
                int addedCount = await _bookRatingRepository.AddAsync(newRating, cancellationToken); // Lưu rating mới
                if (addedCount <= 0)
                {
                    // await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to add rating: AddAsync returned 0.");
                    return (false, null, "Failed to save rating.");
                }

                // >>> CẬP NHẬT LẠI BOOK <<<
                bool updateSuccess = await UpdateBookRatingSummaryAsync(ratingDto.BookId, cancellationToken);
                if (!updateSuccess)
                {
                    // await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to update book rating summary after adding new rating.");
                    // Quyết định xem có nên rollback không hay chỉ cảnh báo
                    return (false, null, "Failed to update book summary after saving rating.");
                }

                // await transaction.CommitAsync(cancellationToken); // Commit transaction

                _logger.LogInformation("Successfully added rating ID {RatingId} and updated book summary for BookId {BookId}.", newRating.Id, ratingDto.BookId);

                // Tạo DTO trả về - Lấy thông tin User và Book nếu cần
                //var createdRating = await _bookRatingRepository.GetByIdAsync(newRating.Id, cancellationToken: cancellationToken); // Giả sử repo hỗ trợ include

                var createdRating = await _bookRatingRepository.GetAllQueryable() // Giả sử repo có GetQueryable
                                .Include(br => br.User)
                                .Include(br => br.Book)
                                .FirstOrDefaultAsync(br => br.Id == newRating.Id, cancellationToken);
                var resultDto = MapToDto(createdRating); // Dùng hàm helper

                return (true, resultDto, null);
            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync(cancellationToken); // Rollback nếu có lỗi
                _logger.LogError(ex, "Error adding rating for BookId {BookId} by UserId {UserId}", ratingDto.BookId, userId);
                return (false, null, "An error occurred while adding the rating.");
            }
        }

        #region MayUseLater
        //public async Task<IEnumerable<BookRatingDto>> GetRatingsForBookAsync(int bookId, CancellationToken cancellationToken = default)
        //{
        //    _logger.LogInformation("Fetching ratings for BookId {BookId}", bookId);
        //    try
        //    {
        //        var ratings = await _bookRatingRepository.GetRatingsForBookAsync(bookId, includeUser: true, cancellationToken: cancellationToken);
        //        var ratingDtos = ratings.Select(r => MapToDto(r)).ToList();
        //        return ratingDtos;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching ratings for BookId {BookId}", bookId);
        //        return new List<BookRatingDto>();
        //    }
        //} 
        #endregion


        public async Task<PagedResult<BookRatingDto>> GetRatingsForBookAsync(int bookId, PaginationParameters paginationParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching ratings for BookId {BookId}, Page {Page}, PageSize {PageSize}",
                bookId, paginationParams.Page, paginationParams.PageSize);
            try
            {
                var query = _bookRatingRepository.GetAllQueryable()
                                              .Where(r => r.BookID == bookId)
                                              .Include(r => r.User) // Include User để lấy UserName
                                              .Include(r => r.Book) // Include Book để lấy Title
                                              .OrderByDescending(r => r.RatingDate); // Sắp xếp

                var totalItems = await query.CountAsync(cancellationToken);

                var ratings = await query
                    .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize)
                    .ToListAsync(cancellationToken);

                var ratingDtos = ratings.Select(r => MapToDto(r)).ToList();

                var pagedResult = new PagedResult<BookRatingDto>(ratingDtos, paginationParams.Page, paginationParams.PageSize, totalItems);
                return pagedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ratings for BookId {BookId}", bookId);
                // Trả về kết quả rỗng thay vì ném lỗi để controller xử lý
                return new PagedResult<BookRatingDto>(new List<BookRatingDto>(), paginationParams.Page, paginationParams.PageSize, 0);
            }
        }

        //// --- Hàm helper tính toán và cập nhật Book ---
        private async Task<bool> UpdateBookRatingSummaryAsync(int bookId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating rating summary for Book ID: {BookId}", bookId);
            try
            {
                // Lấy tất cả StarRating của sách
                var allRatings = await _bookRatingRepository.GetAllRatingsForBookCalculationAsync(bookId, cancellationToken);

                int newRatingCount = allRatings.Count;
                decimal newAverageRating = (newRatingCount > 0)
                    ? Math.Round(allRatings.Average(r => (decimal)r.StarRating), 2) // Tính trung bình, làm tròn 2 chữ số
                    : 0.0m; // Nếu chưa có đánh giá nào thì là 0

                // Lấy sách để cập nhật
                var bookToUpdate = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
                if (bookToUpdate == null)
                {
                    _logger.LogWarning("Could not find Book with ID {BookId} to update rating summary.", bookId);
                    return false; // Báo lỗi không tìm thấy sách
                }

                // Cập nhật thông tin rating cho sách
                bookToUpdate.RatingCount = newRatingCount;
                bookToUpdate.AverageRating = newAverageRating;
                bookToUpdate.UpdatedAt = DateTime.UtcNow;

                // Lưu thay đổi cho sách (Repo auto-save)
                await _bookRepository.UpdateAsync(bookToUpdate, cancellationToken);
                _logger.LogInformation("Successfully updated Book {BookId} rating summary: Count={RatingCount}, Average={AverageRating}", bookId, newRatingCount, newAverageRating);
                return true; // Trả về true nếu thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating summary for Book ID: {BookId}", bookId);
                return false; // Trả về false nếu có lỗi
            }
        }

        // --- Hàm helper để map Entity sang DTO ---
        private BookRatingDto MapToDto(BookRating? rating)
        {
            if (rating == null) return null!;
            return new BookRatingDto
            {
                Id = rating.Id,
                BookId = rating.BookID,
                BookTitle = rating.Book?.Title, // Cần Include Book
                UserId = rating.UserID,
                UserFullName = rating.User?.FullName, // Cần Include User
                StarRating = rating.StarRating,
                Comment = rating.Comment,
                RatingDate = rating.RatingDate
            };
        }
    }
}
