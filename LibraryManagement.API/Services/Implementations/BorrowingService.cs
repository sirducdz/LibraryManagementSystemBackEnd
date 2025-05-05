using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Borrowing;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        private const int ExtensionDays = 7;

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
                    BookID = bookId,
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
                        Id = d.Id,
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
        public async Task<PagedResult<BorrowingRequestDto>> GetMyRequestsAsync(int userId, BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching requests for User {UserId}, Page {Page}, PageSize {PageSize}", userId, queryParams.Page, queryParams.PageSize);
            try
            {
                var query = _borrowingRequestRepository.GetAllQueryable()
                                                     // Bỏ qua filter mềm (nếu có)
                                                     .IgnoreQueryFilters()
                                                    .Where(r => r.RequestorID == userId)
                                                    .Include(r => r.Requestor) // Include để lấy tên
                                                    .Include(r => r.Details)!
                                                    .ThenInclude(d => d.Book) // Include để lấy chi tiết sách
                                                    .OrderByDescending(r => r.DateRequested).AsQueryable(); // Sắp xếp mới nhất trước

                // --- Áp dụng Filter từ queryParams (TƯƠNG TỰ GetAllRequestsAsync) ---
                //if (queryParams.Status.HasValue) // Lọc theo trạng thái nếu được cung cấp
                //    query = query.Where(r => r.Status == queryParams.Status.Value); // So sánh Enum

                if (queryParams.Statuses?.Any() == true) // Dùng ?.Any() để kiểm tra null và rỗng an toàn
                {
                    // Lọc những request có Status nằm trong danh sách Statuses được cung cấp
                    query = query.Where(r => queryParams.Statuses.Contains(r.Status)); // << Dùng Contains()
                }
                if (queryParams.DateFrom.HasValue)
                    query = query.Where(r => r.DateRequested >= queryParams.DateFrom.Value);

                if (queryParams.DateTo.HasValue)
                    query = query.Where(r => r.DateRequested < queryParams.DateTo.Value.AddDays(1)); // Đến hết ngày DateTo

                Expression<Func<BookBorrowingRequest, object>> keySelector = queryParams.SortBy?.ToLowerInvariant() switch
                {
                    // Thêm các trường muốn sort ở đây nếu cần (ví dụ: Status, DueDate)
                    "status" => r => r.Status,
                    "duedate" => r => r.DueDate ?? DateTime.MinValue, // Xử lý DueDate null
                    _ => r => r.DateRequested // Mặc định sort theo DateRequested
                };

                if (string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderByDescending(keySelector);
                }
                else
                {
                    query = query.OrderBy(keySelector);
                }



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
                _logger.LogError(ex, "Error fetching requests for User {UserId}", userId);
                return new PagedResult<BorrowingRequestDto>(new List<BorrowingRequestDto>(), queryParams.Page, queryParams.PageSize, 0);
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
                if (queryParams.Statuses?.Any() == true) // Dùng ?.Any() để kiểm tra null và rỗng an toàn
                {
                    // Lọc những request có Status nằm trong danh sách Statuses được cung cấp
                    query = query.Where(r => queryParams.Statuses.Contains(r.Status)); // << Dùng Contains()
                }
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
                foreach (var detail in request.Details)
                {
                    // Cập nhật lại trạng thái sách trong chi tiết
                    detail.DueDate = request.DueDate; // Gán ID request vào detail
                    detail.OriginalDueDate = request.DueDate; // Chưa trả sách
                }
                await _borrowingDetailsRepository.UpdateRangeAsync(request.Details, cancellationToken); // Repo auto-save
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
                DateProcessed = request.DateProcessed,
                RejectionReason = request.RejectionReason,
                Status = request.Status.ToString(),
                DueDate = request.DueDate,
                Details = request.Details?.Select(d => new BorrowingRequestDetailDto
                {
                    Id = d.Id,
                    BookId = d.BookID,
                    BookTitle = d.Book?.Title // Cần Include Details.Book
                }).ToList() ?? new List<BorrowingRequestDetailDto>(),
                // Thêm thông tin người duyệt nếu cần
                ApproverId = request.ApproverID,
                ApproverName = includeApprover ? request.Approver?.FullName : null, // Cần Include ApproverUser
                // DateProcessed = request.DateProcessed,
                // DueDate = request.DueDate
            };
            return dto;
        }

        public async Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> CancelRequestAsync(int requestId, int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {UserId} attempting to cancel borrowing request ID {RequestId}", userId, requestId);

            try
            {
                // Tìm request trong DB
                // Include các thông tin cần thiết để trả về DTO đầy đủ
                var request = await _borrowingRequestRepository.GetAllQueryable().IgnoreQueryFilters()
                                    .Include(r => r.Requestor)
                                    .Include(r => r.Details)!
                                        .ThenInclude(d => d.Book)
                                    .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

                // 1. Kiểm tra request tồn tại
                if (request == null)
                {
                    _logger.LogWarning("Cancel request failed: Request ID {RequestId} not found.", requestId);
                    return (false, null, "Borrowing request not found.");
                }

                // 2. Kiểm tra quyền sở hữu
                if (request.RequestorID != userId)
                {
                    _logger.LogWarning("Cancel request failed: User {UserId} is not authorized to cancel Request ID {RequestId} (Owner is {OwnerId}).", userId, requestId, request.RequestorID);
                    // Trả về lỗi Forbidden (403) ở Controller sẽ hợp lý hơn, nhưng service trả về lỗi logic
                    return (false, null, "You are not authorized to cancel this request.");
                }

                // 3. Kiểm tra trạng thái có phải là "Waiting" không
                if (request.Status != BorrowingStatus.Waiting)
                {
                    _logger.LogWarning("Cancel request failed: Request ID {RequestId} is not in '{Status}' status (Current: {CurrentStatus}).", requestId, BorrowingStatus.Waiting, request.Status);
                    return (false, null, $"Only requests with status '{BorrowingStatus.Waiting}' can be cancelled.");
                }

                // --- Cập nhật trạng thái ---
                request.Status = BorrowingStatus.Cancelled; // <<< Đặt trạng thái là Cancelled
                request.UpdatedAt = DateTime.UtcNow;
                // Có thể xóa ApproverID, DateProcessed, DueDate, RejectionReason nếu muốn
                // request.ApproverID = null;
                // request.DateProcessed = null;
                // request.DueDate = null;
                // request.RejectionReason = null; // Xóa lý do từ chối cũ nếu có

                // Lưu thay đổi (Repo auto-save hoặc cần SaveChanges)
                int updatedCount = await _borrowingRequestRepository.UpdateAsync(request, cancellationToken);
                if (updatedCount <= 0)
                {
                    _logger.LogError("Failed to cancel request {RequestId}: UpdateAsync returned 0.", requestId);
                    return (false, null, "Failed to update request status.");
                }


                _logger.LogInformation("Request ID {RequestId} cancelled successfully by User {UserId}", requestId, userId);

                // Map và trả về DTO đã cập nhật
                return (true, MapToDto(request), null); // Dùng hàm MapToDto đã có
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling request ID {RequestId} for User {UserId}", requestId, userId);
                return (false, null, "An error occurred while cancelling the request.");
            }
        }
        public async Task<BorrowingRequestDetailViewDto?> GetRequestByIdAsync(int requestId, int userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to fetch detailed borrowing request for ID: {RequestId} by User: {UserId} (IsAdmin: {IsAdmin})", requestId, userId, isAdmin);

            try
            {
                // Query request theo ID, include tất cả thông tin cần thiết
                var request = await _borrowingRequestRepository.GetAllQueryable().IgnoreQueryFilters()
                                     .Include(r => r.Requestor)       // Include User người yêu cầu
                                     .Include(r => r.Approver)    // Include User người duyệt
                                     .Include(r => r.Details)!        // Include danh sách chi tiết
                                         .ThenInclude(d => d.Book) // Include thông tin sách từ chi tiết
                                     .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

                // 1. Kiểm tra tồn tại
                if (request == null)
                {
                    _logger.LogWarning("Get request details failed: Request ID {RequestId} not found.", requestId);
                    return null;
                }

                // 2. Kiểm tra quyền truy cập
                if (!isAdmin && request.RequestorID != userId)
                {
                    _logger.LogWarning("Get request details failed: User {UserId} is not authorized to view Request ID {RequestId} (Owner is {OwnerId}).", userId, requestId, request.RequestorID);
                    return null; // Trả về null để Controller xử lý 403 hoặc 404
                }

                // 3. Map sang DTO chi tiết mới
                var detailDto = new BorrowingRequestDetailViewDto
                {
                    Id = request.Id,
                    DateRequested = request.DateRequested,
                    Status = request.Status.ToString(), // Chuyển enum thành string
                    DueDate = request.DueDate,
                    DateProcessed = request.DateProcessed,
                    RejectionReason = request.RejectionReason,

                    RequestorId = request.RequestorID,
                    RequestorFullName = request.Requestor?.FullName, // Lấy từ User đã include
                    RequestorEmail = request.Requestor?.Email,     // Lấy từ User đã include

                    ApproverId = request.ApproverID,
                    ApproverFullName = request.Approver?.FullName, // Lấy từ User đã include
                    // ApproverEmail = request.ApproverUser?.Email,

                    Details = request.Details.Select(d => new BookInfoForRequestDetailDto
                    {
                        DetailId = d.Id,
                        BookId = d.BookID,
                        Title = d.Book?.Title ?? "N/A", // Lấy từ Book đã include
                        Author = d.Book?.Author,        // Lấy từ Book đã include
                        CoverImageUrl = d.Book?.CoverImageUrl, // Lấy từ Book đã include
                        ReturnedDate = d.ReturnedDate,    // Lấy ngày trả từ Detail
                        isExtended = d.IsExtensionUsed,
                        DueDate = d.DueDate,
                        OriginalDate = d.OriginalDueDate,
                    }).ToList() ?? new List<BookInfoForRequestDetailDto>()
                };

                _logger.LogInformation("Successfully fetched detailed view for request ID {RequestId}", requestId);
                return detailDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching detailed borrowing request for ID: {RequestId}", requestId);
                // Ném lỗi hoặc trả về null tùy cách xử lý lỗi chung
                // Trả về null để Controller trả 500 nếu muốn ẩn chi tiết lỗi
                return null;
            }
        }
        public async Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> ExtendDueDateAsync(int detailId, int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {UserId} attempting to extend due date for borrowing detail ID {DetailId}", userId, detailId);

            try
            {
                // Tìm chi tiết mượn sách, include Request cha để kiểm tra quyền và trạng thái
                var detailToExtend = await _borrowingDetailsRepository.GetAllQueryable(true)
                                           .IgnoreQueryFilters()
                                           .Include(d => d.Request) // Include Request cha
                                                .ThenInclude(r => r!.Requestor) // Include người mượn từ Request
                                           .Include(d => d.Request!.Approver) // Include người duyệt
                                           .Include(d => d.Request!.Details)! // Include tất cả details của request cha
                                                .ThenInclude(sd => sd.Book) // Include sách từ detail
                                           .FirstOrDefaultAsync(d => d.Id == detailId, cancellationToken);

                // 1. Kiểm tra tồn tại
                if (detailToExtend == null)
                {
                    _logger.LogWarning("Extend due date failed: Detail ID {DetailId} not found.", detailId);
                    return (false, null, "Borrowing record detail not found.");
                }

                var parentRequest = detailToExtend.Request; // Lấy request cha

                if (parentRequest == null) // Kiểm tra null phòng trường hợp dữ liệu lỗi
                {
                    _logger.LogError("Extend due date failed: Parent request is null for Detail ID {DetailId}.", detailId);
                    return (false, null, "Invalid borrowing record state.");
                }

                // 2. Kiểm tra quyền sở hữu
                if (parentRequest.RequestorID != userId)
                {
                    _logger.LogWarning("Extend due date failed: User {UserId} is not authorized for Detail ID {DetailId} (Owner is {OwnerId}).", userId, detailId, parentRequest.RequestorID);
                    return (false, null, "You are not authorized to extend this item."); // Lỗi Forbidden 403 ở Controller
                }

                // 3. Kiểm tra trạng thái Request cha
                if (parentRequest.Status != BorrowingStatus.Approved)
                {
                    _logger.LogWarning("Extend due date failed: Request ID {RequestId} for Detail ID {DetailId} is not in Approved status (Current: {Status}).", parentRequest.Id, detailId, parentRequest.Status);
                    return (false, null, "Cannot extend item for a request that is not approved.");
                }

                // 4. Kiểm tra sách đã trả chưa
                if (detailToExtend.ReturnedDate.HasValue)
                {
                    _logger.LogWarning("Extend due date failed: Book in Detail ID {DetailId} was already returned.", detailId);
                    return (false, null, "Cannot extend item that has already been returned.");
                }

                // 5. Kiểm tra đã dùng quyền gia hạn chưa
                if (detailToExtend.IsExtensionUsed)
                {
                    _logger.LogWarning("Extend due date failed: Extension already used for Detail ID {DetailId}.", detailId);
                    return (false, null, "You have already used the one-time extension for this item.");
                }

                // 6. Kiểm tra DueDate có tồn tại không
                if (!detailToExtend.DueDate.HasValue)
                {
                    _logger.LogError("Extend due date failed: DueDate is null for Detail ID {DetailId}. Approval process might be incomplete.", detailId);
                    return (false, null, "Cannot extend item: missing current due date.");
                }

                // 7. (Tùy chọn) Kiểm tra điều kiện khác, ví dụ: không cho gia hạn nếu đã quá hạn
                // if (detailToExtend.DueDate.Value < DateTime.UtcNow)
                // {
                //     _logger.LogWarning("Extend due date failed: Item for Detail ID {DetailId} is overdue.", detailId);
                //     return (false, null, "Cannot extend overdue items.");
                // }


                // --- Nếu tất cả hợp lệ -> Thực hiện gia hạn ---
                DateTime newDueDate = detailToExtend.DueDate.Value.AddDays(ExtensionDays); // Tính ngày hết hạn mới

                detailToExtend.OriginalDueDate ??= detailToExtend.DueDate; // Lưu lại DueDate gốc nếu chưa có
                detailToExtend.DueDate = newDueDate;
                detailToExtend.IsExtensionUsed = true; // Đánh dấu đã sử dụng quyền gia hạn
                // Cập nhật UpdatedAt cho Request cha (vì một chi tiết của nó thay đổi)
                parentRequest.UpdatedAt = DateTime.UtcNow;

                // EF Core sẽ tự động theo dõi thay đổi trên detailToExtend và parentRequest vì chúng ta đã query và include chúng
                await _borrowingDetailsRepository.UpdateAsync(detailToExtend); // Cập nhật detail

                _logger.LogInformation("Successfully extended due date for Detail ID {DetailId} to {NewDueDate} by User {UserId}", detailId, newDueDate, userId);

                // Trả về DTO của Request cha đã được cập nhật (ít nhất là UpdatedAt)
                return (true, MapToDto(parentRequest, includeApprover: true), null);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending due date for Detail ID {DetailId} by User {UserId}", detailId, userId);
                return (false, null, "An error occurred while extending the due date.");
            }
        }
    }
}
