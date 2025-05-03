using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Borrowing;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Services.Implementations
{
    public class BorrowingService : IBorrowingService
    {
        private readonly IBookBorrowingRequestRepository _borrowingRequestRepository;
        private readonly IBookRepository _bookRepository; // Để kiểm tra sách tồn tại/available
        private readonly IUserRepository _userRepository; // Để lấy tên user (tùy chọn)
        private readonly ILogger<BorrowingService> _logger;
        private readonly IBookBorrowingRequestDetailsRepository _borrowingDetailsRepository;
        private const int DefaultBorrowingDays = 14;

        public BorrowingService(
            IBookBorrowingRequestRepository borrowingRequestRepository,
            IBookRepository bookRepository,
            IUserRepository userRepository,
            ILogger<BorrowingService> logger
,
            IBookBorrowingRequestDetailsRepository borrowingDetailsRepository
/*, LibraryDbContext context */)
        {
            _borrowingRequestRepository = borrowingRequestRepository;
            _bookRepository = bookRepository;
            _userRepository = userRepository;
            _logger = logger;
            _borrowingDetailsRepository = borrowingDetailsRepository;
            // _context = context;
        }

        public async Task<(bool Success, BorrowingRequestDto? CreatedRequest, string? ErrorMessage)> CreateRequestAsync(CreateBorrowingRequestDto requestDto, int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {UserId} attempting to create borrowing request for books: {@BookIds}", userId, requestDto.BookIds);

            // --- 1. Kiểm tra số lượng sách (FluentValidation đã check nhưng kiểm tra lại) ---
            if (requestDto.BookIds == null || !requestDto.BookIds.Any())
                return (false, null, "Book list cannot be empty.");
            if (requestDto.BookIds.Count > 5) //[cite: 2]
                return (false, null, "Cannot request more than 5 books at once.");
            if (requestDto.BookIds.Distinct().Count() != requestDto.BookIds.Count)
                return (false, null, "Book list contains duplicate IDs.");

            // --- 2. Kiểm tra giới hạn request hàng tháng (3 approved requests/month) --- [cite: 3]
            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;
            try
            {
                int approvedRequestsThisMonth = await _borrowingRequestRepository.CountActiveRequestsForUserInMonthAsync(userId, currentYear, currentMonth, cancellationToken);
                if (approvedRequestsThisMonth >= 3)
                {
                    _logger.LogWarning("User {UserId} reached monthly borrowing limit (Month: {Month}, Year: {Year})", userId, currentMonth, currentYear);
                    return (false, null, "Monthly borrowing request limit (3 request) reached.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking monthly request limit for User {UserId}", userId);
                return (false, null, "Could not verify monthly borrowing limit.");
            }


            // --- 3. Kiểm tra sách tồn tại và có sẵn ---
            var distinctBookIds = requestDto.BookIds.Distinct().ToList();
            var booksFromDb = await _bookRepository.GetAllQueryable()
                                     .Where(b => distinctBookIds.Contains(b.Id))//&& !b.IsDeleted
                                     .Include(b => b.BorrowingDetails!) // Include để tính Available
                                        .ThenInclude(d => d.Request)
                                     .ToListAsync(cancellationToken);

            // Kiểm tra xem có tìm thấy đủ số lượng sách không
            if (booksFromDb.Count != distinctBookIds.Count)
            {
                var foundIds = booksFromDb.Select(b => b.Id).ToList();
                var notFoundIds = distinctBookIds.Except(foundIds);
                _logger.LogWarning("Create request failed: Books not found or deleted: {@NotFoundBookIds}", notFoundIds);
                return (false, null, $"Could not find or access books with IDs: {string.Join(", ", notFoundIds)}");
            }

            // Kiểm tra số lượng có sẵn từng cuốn
            var unavailableBooks = new List<string>();
            foreach (var book in booksFromDb)
            {
                int currentlyBorrowedCount = book.BorrowingDetails
                       .Count(detail => detail.Request?.Status == BorrowingStatus.Approved && detail.ReturnedDate == null);
                int availableQuantity = book.TotalQuantity - currentlyBorrowedCount;
                if (availableQuantity <= 0)
                {
                    unavailableBooks.Add($"'{book.Title}' (ID: {book.Id})");
                }
            }
            if (unavailableBooks.Any())
            {
                _logger.LogWarning("Create request failed for User {UserId}: Books unavailable: {UnavailableBooks}", userId, unavailableBooks);
                return (false, null, $"The following books are currently unavailable: {string.Join(", ", unavailableBooks)}");
            }


            // --- 4. Tạo Entities ---
            var newRequest = new BookBorrowingRequest
            {
                RequestorID = userId,
                DateRequested = now,
                Status = BorrowingStatus.Waiting, // <<< Trạng thái chờ duyệt ban đầu (dùng Enum/Constant)
                CreatedAt = now,
                Details = new List<BookBorrowingRequestDetails>() // Khởi tạo list details
            };

            foreach (var bookId in distinctBookIds)
            {
                newRequest.Details.Add(new BookBorrowingRequestDetails
                {
                    BookID = bookId
                    // RequestID sẽ được EF Core tự gán khi AddAsync Request cha
                    // ReturnedDate là null ban đầu
                });
            }

            // --- 5. Lưu vào Database (Nên dùng Transaction) ---
            // using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Lưu request cha (bao gồm cả details nếu cấu hình đúng hoặc repo xử lý)
                int addedCount = await _borrowingRequestRepository.AddAsync(newRequest, cancellationToken); // Repo auto-save

                if (addedCount <= 0)
                {
                    // await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Failed to create borrowing request for User {UserId}: AddAsync returned 0.", userId);
                    return (false, null, "Failed to save borrowing request.");
                }

                // await transaction.CommitAsync(cancellationToken); // Commit transaction

                _logger.LogInformation("Successfully created borrowing request ID {RequestId} for User {UserId}", newRequest.Id, userId);

                // --- 6. Map và trả về DTO ---
                // Lấy lại thông tin đầy đủ để trả về (bao gồm tên User, tên Sách)
                var createdRequest = await _borrowingRequestRepository.GetAllQueryable()
                                        .Include(r => r.Requestor) // Include User
                                        .Include(r => r.Details)! // Include Details
                                            .ThenInclude(d => d.Book) // Include Book từ Details
                                        .FirstOrDefaultAsync(r => r.Id == newRequest.Id, cancellationToken);

                if (createdRequest == null) // Kiểm tra lại cho chắc
                {
                    _logger.LogError("Could not retrieve created borrowing request ID {RequestId}", newRequest.Id);
                    return (false, null, "Failed to retrieve request details after creation.");
                }

                var resultDto = new BorrowingRequestDto
                {
                    Id = createdRequest.Id,
                    RequestorId = createdRequest.RequestorID,
                    RequestorName = createdRequest.Requestor?.FullName, // Lấy tên User
                    DateRequested = createdRequest.DateRequested,
                    Status = createdRequest.Status.ToString(),
                    Details = createdRequest.Details.Select(d => new BorrowingRequestDetailDto
                    {
                        BookId = d.BookID,
                        BookTitle = d.Book?.Title // Lấy tên sách
                    }).ToList()
                };

                return (true, resultDto, null);

            }
            catch (Exception ex)
            {
                // await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating borrowing request for User {UserId}", userId);
                return (false, null, "An unexpected error occurred while creating the request.");
            }
        }

        // ... (Các phương thức service khác) ...
        public async Task<PagedResult<BorrowingRequestDto>> GetMyRequestsAsync(int userId, PaginationParameters paginationParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching requests for User {UserId}, Page {Page}, PageSize {PageSize}", userId, paginationParams.Page, paginationParams.PageSize);
            try
            {
                var query = _borrowingRequestRepository.GetAllQueryable()
                                                     // Bỏ qua filter mềm (nếu có)
                                                     .IgnoreQueryFilters()
                                                    .Where(r => r.RequestorID == userId)
                                                    .Include(r => r.Requestor) // Include để lấy tên
                                                    .Include(r => r.Details)!
                                                    .ThenInclude(d => d.Book) // Include để lấy chi tiết sách
                                                    .OrderByDescending(r => r.DateRequested); // Sắp xếp mới nhất trước

                var totalItems = await query.CountAsync(cancellationToken);

                var requests = await query
                    .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize)
                    .ToListAsync(cancellationToken);

                var requestDtos = requests.Select(r => MapToDto(r)).ToList();
                return new PagedResult<BorrowingRequestDto>(requestDtos, paginationParams.Page, paginationParams.PageSize, totalItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching requests for User {UserId}", userId);
                return new PagedResult<BorrowingRequestDto>(new List<BorrowingRequestDto>(), paginationParams.Page, paginationParams.PageSize, 0);
            }
        }

        public async Task<PagedResult<BorrowingRequestDto>> GetAllRequestsAsync(BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all requests with parameters: {@QueryParams}", queryParams);
            try
            {
                var query = _borrowingRequestRepository.GetAllQueryable()
                                                    .IgnoreQueryFilters() // Bỏ qua filter mềm (nếu có)
                                                    .Include(r => r.Requestor) // Include để lấy tên
                                                    .Include(r => r.Details)!
                                                        .ThenInclude(d => d.Book)
                                                    .AsQueryable(); // Include để lấy chi tiết sách

                // --- Áp dụng Filter ---
                if (queryParams.UserId.HasValue)
                    query = query.Where(r => r.RequestorID == queryParams.UserId.Value);
                if (queryParams.Status.HasValue)
                    query = query.Where(r => r.Status == queryParams.Status);
                if (queryParams.DateFrom.HasValue)
                    query = query.Where(r => r.DateRequested >= queryParams.DateFrom.Value);
                if (queryParams.DateTo.HasValue)
                    query = query.Where(r => r.DateRequested < queryParams.DateTo.Value.AddDays(1)); // Đến hết ngày DateTo

                // --- Áp dụng Sorting ---
                // Nên có logic phức tạp hơn để xử lý SortBy và SortOrder
                if (string.Equals(queryParams.SortOrder, "asc", StringComparison.OrdinalIgnoreCase))
                    query = query.OrderBy(r => r.DateRequested);
                else
                    query = query.OrderByDescending(r => r.DateRequested);


                var totalItems = await query.CountAsync(cancellationToken);

                var requests = await query
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToListAsync(cancellationToken);

                var requestDtos = requests.Select(r => MapToDto(r)).ToList();
                return new PagedResult<BorrowingRequestDto>(requestDtos, queryParams.Page, queryParams.PageSize, totalItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all requests with query parameters: {@QueryParams}", queryParams);
                return new PagedResult<BorrowingRequestDto>(new List<BorrowingRequestDto>(), queryParams.Page, queryParams.PageSize, 0);
            }
        }

        public async Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> ApproveRequestAsync(int requestId, int approverUserId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {ApproverUserId} attempting to approve request ID {RequestId}", approverUserId, requestId);

            try
            {
                var request = await _borrowingRequestRepository.GetAllQueryable()
                                     .IgnoreQueryFilters()
                                     .Include(r => r.Details)!
                                        .ThenInclude(d => d.Book) // Include Details và Book
                                     .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

                if (request == null) return (false, null, "Request not found.");
                if (request.Status != BorrowingStatus.Waiting) return (false, null, $"Request is not in '{BorrowingStatus.Waiting}' status.");

                // --- KIỂM TRA LẠI SỐ LƯỢNG SÁCH TRƯỚC KHI DUYỆT ---
                var bookUpdates = new List<Book>();
                foreach (var detail in request.Details)
                {
                    var book = detail.Book; // Sách đã được include
                    if (book == null || book.IsDeleted)
                    {
                        //return (false, null, $"Book with ID {detail.BookID} in the request not found or deleted.");
                        return (false, null, $"Cannot approve: Book '{book?.Title ?? "ID:" + detail.BookID}' is unavailable/deleted. Please reject.");
                    }

                    // Cần tính toán lại số lượng khả dụng chính xác tại thời điểm này
                    int currentlyBorrowedCount = await _borrowingDetailsRepository.CountActiveBorrowsForBookAsync(book.Id, cancellationToken);

                    int availableQuantity = book.TotalQuantity - currentlyBorrowedCount;

                    if (availableQuantity <= 0)
                    {
                        _logger.LogWarning("Approve request {RequestId} failed: Book '{BookTitle}' (ID:{BookId}) is unavailable.", requestId, book.Title, book.Id);
                        return (false, null, $"Book '{book.Title}' is currently unavailable.");
                    }
                    // Nếu có cột AvailableQuantity thì giảm nó đi, nhưng cách này không an toàn với concurrency
                    // book.AvailableQuantity -= 1;
                    // bookUpdates.Add(book);
                }
                // Lưu ý: Việc giảm AvailableQuantity ở đây có thể gây ra race condition.
                // Cách an toàn hơn là kiểm tra số lượng khi mượn và tăng/giảm khi trả/mượn.
                // Trong phạm vi bài này, chỉ kiểm tra là đủ. Việc cập nhật số lượng nên thực hiện khi trả sách.

                // --- Cập nhật Request ---
                request.Status = BorrowingStatus.Approved;
                request.ApproverID = approverUserId; // << Lưu ID người duyệt
                request.DateProcessed = DateTime.UtcNow; // Ngày xử lý
                                                         // Tính ngày hết hạn (DueDate) - Có thể cấu hình số ngày
                request.DueDate = DateTime.UtcNow.AddDays(DefaultBorrowingDays); // Ví dụ: 14 ngày

                await _borrowingRequestRepository.UpdateAsync(request, cancellationToken); // Repo auto-save Request
                _logger.LogInformation("Request ID {RequestId} approved successfully by User {ApproverUserId}", requestId, approverUserId);

                // Lấy lại thông tin mới nhất để trả về
                var updatedRequest = await _borrowingRequestRepository.GetAllQueryable()
                                          .Include(r => r.Requestor)
                                          .Include(r => r.Approver) // Include cả Approver
                                          .Include(r => r.Details)!
                                            .ThenInclude(d => d.Book)
                                          .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

                return (true, MapToDto(updatedRequest, includeApprover: true), null); // Trả về DTO đã cập nhật
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request ID {RequestId}", requestId);
                return (false, null, "An error occurred while approving the request.");
            }
        }

        public async Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> RejectRequestAsync(int requestId, int approverUserId, string? reason, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {ApproverUserId} attempting to reject request ID {RequestId}", approverUserId, requestId);
            try
            {
                var request = await _borrowingRequestRepository.GetByIdAsync(requestId, cancellationToken);

                if (request == null)
                    return (false, null, "Request not found.");
                if (request.Status != BorrowingStatus.Waiting)
                    return (false, null, $"Request is not in '{BorrowingStatus.Waiting}' status.");

                request.Status = BorrowingStatus.Rejected;
                request.ApproverID = approverUserId;
                request.DateProcessed = DateTime.UtcNow;
                request.DueDate = null; // Không có hạn trả cho request bị từ chối
                request.RejectionReason = reason?.Trim();

                await _borrowingRequestRepository.UpdateAsync(request, cancellationToken); // Repo auto-save

                _logger.LogInformation("Request ID {RequestId} rejected successfully by User {ApproverUserId}", requestId, approverUserId);

                var updatedRequest = await _borrowingRequestRepository.GetAllQueryable()
                                           .Include(r => r.Requestor)
                                           .Include(r => r.Approver)
                                           .Include(r => r.Details)!
                                           .ThenInclude(d => d.Book)
                                           .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

                return (true, MapToDto(updatedRequest, includeApprover: true), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request ID {RequestId}", requestId);
                return (false, null, "An error occurred while rejecting the request.");
            }
        }

        // Hàm MapToDto cần cập nhật để lấy thêm tên sách, tên người mượn, tên người duyệt
        private BorrowingRequestDto MapToDto(BookBorrowingRequest? request, bool includeApprover = false)
        {
            if (request == null) return null!;
            var dto = new BorrowingRequestDto
            {
                Id = request.Id,
                RequestorId = request.RequestorID,
                RequestorName = request.Requestor?.FullName, // Cần Include Requestor
                DateRequested = request.DateRequested,
                RejectionReason = request.RejectionReason,
                Status = request.Status.ToString(),
                Details = request.Details?.Select(d => new BorrowingRequestDetailDto
                {
                    BookId = d.BookID,
                    BookTitle = d.Book?.Title // Cần Include Details.Book
                }).ToList() ?? new List<BorrowingRequestDetailDto>()
                // Thêm thông tin người duyệt nếu cần
                // ApproverId = request.ApproverID,
                // ApproverName = includeApprover ? request.ApproverUser?.FullName : null, // Cần Include ApproverUser
                // DateProcessed = request.DateProcessed,
                // DueDate = request.DueDate
            };
            return dto;
        }
    }
}
