using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Book;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LibraryManagement.API.Services.Implementations
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository; // Dùng IBookRepository cụ thể
        private readonly ILogger<BookService> _logger;
        // private readonly IMapper _mapper;

        public BookService(IBookRepository bookRepository, ILogger<BookService> logger /*, IMapper mapper*/)
        {
            _bookRepository = bookRepository;
            _logger = logger;
            // _mapper = mapper;
        }

        public async Task<PagedResult<BookSummaryDto>> GetBooksAsync(BookQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching books with parameters: Page={Page}, PageSize={PageSize}, CategoryId={CategoryId}",
                queryParams.Page, queryParams.PageSize, queryParams.CategoryId);
            try
            {
                var query = _bookRepository.GetAllQueryable(false)
                                          .Include(b => b.Category)
                                          .Include(b => b.BorrowingDetails!) // Include chi tiết mượn
                                                .ThenInclude(d => d.Request)
                                          .AsQueryable(); // Include Category
                                                          //.Where(b => !b.IsDeleted); // Luôn lọc sách chưa xóa mềm đã cấu hình trong configuration

                // 1. Lọc theo CategoryId
                if (queryParams.CategoryId.HasValue && queryParams.CategoryId > 0)
                {
                    query = query.Where(b => b.CategoryID == queryParams.CategoryId.Value);
                }

                // 2. Lọc theo SearchTerm (Title hoặc Author) - Không phân biệt hoa thường
                if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
                {
                    var term = queryParams.SearchTerm.Trim().ToLower(); // Chuẩn hóa search term
                    query = query.Where(b => b.Title.ToLower().Contains(term) ||
                                             (b.Author != null && b.Author.ToLower().Contains(term)));
                }

                // 3. Lọc theo IsAvailable (Tính toán động trong Where)
                // Lưu ý: Việc lọc dựa trên tính toán từ bảng liên quan như thế này có thể ảnh hưởng hiệu năng.
                // Cần Include BorrowingDetails và Request như đã làm ở trên.
                if (queryParams.IsAvailable.HasValue)
                {
                    if (queryParams.IsAvailable.Value) // Lọc sách CÓ SẴN (Available = true)
                    {
                        query = query.Where(b => (b.TotalQuantity - b.BorrowingDetails
                                                   .Count(detail => detail.Request!.Status == BorrowingStatus.Approved && detail.ReturnedDate == null)) > 0);
                    }
                    else // Lọc sách ĐÃ HẾT (Available = false)
                    {
                        query = query.Where(b => (b.TotalQuantity - b.BorrowingDetails
                                                  .Count(detail => detail.Request!.Status == BorrowingStatus.Approved && detail.ReturnedDate == null)) <= 0);
                    }
                }

                // --- ÁP DỤNG SORTING ---
                // Tạo biểu thức sắp xếp động
                Expression<Func<Book, object>> keySelector = queryParams.SortBy?.ToLowerInvariant() switch
                {
                    "title" => b => b.Title,
                    "author" => b => b.Author ?? "", // Xử lý Author có thể null
                    "rating" => b => b.AverageRating, // Sắp xếp theo điểm trung bình
                    "ratingcount" => b => b.RatingCount, // Sắp xếp theo số lượt đánh giá
                    "createdat" => b => b.CreatedAt, // Sắp xếp theo ngày tạo
                    "year" => b => b.PublicationYear ?? 0, // Sắp xếp theo năm XB (xử lý null)
                    "id" => b => b.Id, // Sắp xếp theo năm XB (xử lý null)
                    _ => b => b.Title // Mặc định sắp xếp theo Title nếu SortBy không hợp lệ hoặc null
                };

                // Áp dụng OrderBy hoặc OrderByDescending
                if (string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderByDescending(keySelector);
                }
                else
                {
                    query = query.OrderBy(keySelector);
                }

                // Thêm sắp xếp phụ để đảm bảo thứ tự ổn định nếu key chính bằng nhau
                //query = query.ThenBy(b => b.Id); // Luôn sắp xếp theo ID làm tiêu chí phụ


                // --- Đếm tổng số lượng item khớp điều kiện LỌC (TRƯỚC KHI PHÂN TRANG) ---
                var totalItems = await query.CountAsync(cancellationToken);
                query = query.Include(b => b.BorrowingDetails)
                 .ThenInclude(d => d.Request);
                // --- Áp dụng Phân trang ---
                var books = await query
                    .Skip((queryParams.Page - 1) * queryParams.PageSize) // Bỏ qua các trang trước
                    .Take(queryParams.PageSize) // Lấy số lượng item cho trang hiện tại
                    .ToListAsync(cancellationToken);

                var bookDtos = books.Select(b =>
                {
                    int currentlyBorrowedCount = b.BorrowingDetails
                        .Count(detail => detail.Request?.Status == BorrowingStatus.Approved
                        && detail.ReturnedDate == null);
                    // Tính số lượng có sẵn động
                    int availableQuantity = b.TotalQuantity - currentlyBorrowedCount;

                    return new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Author = b.Author ?? "Unknown",
                        Category = b.Category?.Name,
                        CategoryId = b.CategoryID, // <<< SỬA LẠI: Lấy từ b.CategoryID, không phải b.Id
                        CoverImageUrl = b.CoverImageUrl,
                        Rating = b.AverageRating,
                        RatingCount = b.RatingCount,
                        Available = availableQuantity > 0,
                        Copies = availableQuantity,
                        PublicationYear = b.PublicationYear,
                    };
                }).ToList();
                // --- Dùng AutoMapper ProjectTo (hiệu quả hơn) ---
                // var bookDtos = await query
                //     .Skip((queryParams.Page - 1) * queryParams.PageSize)
                //     .Take(queryParams.PageSize)
                //     .ProjectTo<BookSummaryDto>(_mapper.ConfigurationProvider) // Cần AutoMapper config
                //     .ToListAsync(cancellationToken);
                // ----------------------------------------------

                //_logger.LogInformation("Successfully fetched page {Page} with {Count} books. Total items: {TotalItems}",
                //    queryParams.Page, bookDtos.Count, totalItems);

                // Tạo và trả về kết quả phân trang
                var pagedResult = new PagedResult<BookSummaryDto>(bookDtos, queryParams.Page, queryParams.PageSize, totalItems);
                return pagedResult;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching books with parameters: {@QueryParams}", queryParams);
                // Ném lỗi để Controller bắt hoặc trả về kết quả lỗi
                throw; // Hoặc return một PagedResult rỗng với thông báo lỗi
            }
        }
        public async Task<BookDetailDto?> GetBookByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching book details for ID: {BookId}", id);
            try
            {
                // Include Category và có thể cả Ratings nếu muốn hiển thị kèm
                var book = await _bookRepository.GetAllQueryable()
                                             .Include(b => b.Category)
                                             .Include(b => b.BorrowingDetails!) // Include để tính AvailableQuantity
                                                 .ThenInclude(d => d.Request)
                                             // .Include(b => b.Ratings) // << Tùy chọn: Include Ratings
                                             .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (book == null)
                {
                    _logger.LogWarning("Book with ID {BookId} not found.", id);
                    return null;
                }

                // Tính toán Available Quantity
                int currentlyBorrowedCount = book.BorrowingDetails
                    .Count(detail => detail.Request?.Status == BorrowingStatus.Approved
                    && detail.ReturnedDate == null);
                int availableQuantity = book.TotalQuantity - currentlyBorrowedCount;

                // Map sang BookDetailDto
                var bookDetailDto = new BookDetailDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    Category = book.Category?.Name,
                    CategoryId = book.CategoryID,
                    ISBN = book.ISBN,
                    Publisher = book.Publisher,
                    PublicationYear = book.PublicationYear,
                    Description = book.Description,
                    CoverImageUrl = book.CoverImageUrl,
                    AverageRating = book.AverageRating,
                    RatingCount = book.RatingCount,
                    TotalQuantity = book.TotalQuantity,
                    AvailableQuantity = availableQuantity, // Gán giá trị tính toán
                    CreatedAt = book.CreatedAt,
                    UpdatedAt = book.UpdatedAt
                    // Map Ratings nếu đã Include và muốn hiển thị
                    // Ratings = book.Ratings.Select(r => new BookRatingDto { ... }).ToList()
                };
                return bookDetailDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching book details for ID: {BookId}", id);
                throw;
            }
        }

        public async Task<(bool Success, BookDetailDto? CreatedBook, string? ErrorMessage)> CreateBookAsync(CreateBookDto bookDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to create a new book with title: {Title}", bookDto.Title);
            try
            {
                // (Tùy chọn) Kiểm tra trùng lặp ISBN hoặc Title/Author nếu cần
                bool exists = await _bookRepository.ExistsAsync(b => b.ISBN == bookDto.ISBN);
                if (exists)
                    return (false, null, "ISBN already exists.");

                // Map DTO sang Entity
                var newBook = new Book
                {
                    Title = bookDto.Title.Trim(),
                    Author = bookDto.Author?.Trim(),
                    ISBN = bookDto.ISBN?.Trim(),
                    Publisher = bookDto.Publisher?.Trim(),
                    PublicationYear = bookDto.PublicationYear,
                    Description = bookDto.Description?.Trim(),
                    CoverImageUrl = bookDto.CoverImageUrl, // Có thể cần validate URL
                    CategoryID = bookDto.CategoryID,
                    TotalQuantity = bookDto.TotalQuantity,
                    AverageRating = 0, // Khởi tạo
                    RatingCount = 0,   // Khởi tạo
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                int addedCount = await _bookRepository.AddAsync(newBook, cancellationToken); // Repo auto-save
                if (addedCount > 0)
                {
                    _logger.LogInformation("Successfully created book with ID: {BookId}", newBook.Id);
                    // Map lại sang DTO để trả về (cần load Category Name)
                    var createdBook = await GetBookByIdAsync(newBook.Id, cancellationToken); // Gọi lại hàm GetById để lấy DTO đầy đủ
                    return (true, createdBook, null);
                }
                else
                {
                    _logger.LogError("Failed to create book: AddAsync returned 0.");
                    return (false, null, "Failed to save the new book.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book with title: {Title}", bookDto.Title);
                return (false, null, "An unexpected error occurred while creating the book.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateBookAsync(int id, UpdateBookDto bookDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to update book with ID: {BookId}", id);
            try
            {
                var bookToUpdate = await _bookRepository.GetByIdAsync(id, cancellationToken); // Lấy entity cần cập nhật
                if (bookToUpdate == null || bookToUpdate.IsDeleted)
                {
                    _logger.LogWarning("Update book failed: Book with ID {BookId} not found.", id);
                    return (false, "Book not found.");
                }

                // (Tùy chọn) Kiểm tra trùng lặp ISBN (nếu ISBN thay đổi và khác null)
                if (!string.IsNullOrEmpty(bookDto.ISBN) && bookDto.ISBN != bookToUpdate.ISBN)
                {
                    bool isbnExists = await _bookRepository.ExistsAsync(b => b.ISBN == bookDto.ISBN && b.Id != id && !b.IsDeleted);
                    if (isbnExists) return (false, "ISBN already exists for another book.");
                }


                // Map các thuộc tính từ DTO vào Entity (Không cập nhật Rating ở đây)
                bookToUpdate.Title = bookDto.Title.Trim();
                bookToUpdate.Author = bookDto.Author?.Trim();
                bookToUpdate.ISBN = bookDto.ISBN?.Trim();
                bookToUpdate.Publisher = bookDto.Publisher?.Trim();
                bookToUpdate.PublicationYear = bookDto.PublicationYear;
                bookToUpdate.Description = bookDto.Description?.Trim();
                bookToUpdate.CoverImageUrl = bookDto.CoverImageUrl;
                bookToUpdate.CategoryID = bookDto.CategoryID;
                bookToUpdate.TotalQuantity = bookDto.TotalQuantity;
                bookToUpdate.UpdatedAt = DateTime.UtcNow;

                int updatedCount = await _bookRepository.UpdateAsync(bookToUpdate, cancellationToken); // Repo auto-save
                if (updatedCount > 0)
                {
                    _logger.LogInformation("Successfully updated book with ID: {BookId}", id);
                    return (true, null);
                }
                else
                {
                    // Có thể xảy ra nếu có lỗi concurrency hoặc không có thay đổi nào thực sự
                    _logger.LogWarning("Update book failed for ID {BookId}: UpdateAsync returned 0.", id);
                    return (false, "Failed to update book. It might have been modified by another user."); // Hoặc thông báo khác
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ID: {BookId}", id);
                return (false, "An unexpected error occurred while updating the book.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteBookAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to delete (soft) book with ID: {BookId}", id);
            try
            {
                var bookToDelete = await _bookRepository.GetByIdAsync(id, cancellationToken);
                if (bookToDelete == null || bookToDelete.IsDeleted) // Kiểm tra cả IsDeleted
                {
                    _logger.LogWarning("Delete book failed: Book with ID {BookId} not found or already deleted.", id);
                    return (false, "Book not found.");
                }

                // Kiểm tra xem sách có đang được mượn không trước khi xóa? (Tùy chọn)
                // int currentlyBorrowedCount = await _context.BookBorrowingRequestDetails.CountAsync(d => d.BookID == id && d.ReturnedDate == null && d.Request.Status == "Approved");
                // if (currentlyBorrowedCount > 0) return (false, "Cannot delete book with active borrowings.");


                // Thực hiện Soft Delete
                bookToDelete.IsDeleted = true;
                bookToDelete.UpdatedAt = DateTime.UtcNow;

                int deletedCount = await _bookRepository.UpdateAsync(bookToDelete, cancellationToken); // Dùng Update để lưu cờ IsDeleted

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Successfully soft-deleted book with ID: {BookId}", id);
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("Soft delete book failed for ID {BookId}: UpdateAsync returned 0.", id);
                    return (false, "Failed to delete book.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID: {BookId}", id);
                return (false, "An unexpected error occurred while deleting the book.");
            }
        }

        // Hàm cập nhật Rating Summary (nếu cần gọi từ BookRatingService)
        // public async Task<bool> UpdateBookRatingSummaryAsync(int bookId, CancellationToken cancellationToken) { ... }
    }
}

