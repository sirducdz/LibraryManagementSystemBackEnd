using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Dashboard;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBookBorrowingRequestRepository _borrowingRequestRepository;
        private readonly IBookBorrowingRequestDetailsRepository _borrowingDetailsRepository;
        private readonly ILogger<DashboardService> _logger;
        // Inject thêm các repo khác nếu cần

        public DashboardService(
            IBookRepository bookRepository,
            IUserRepository userRepository,
            IBookBorrowingRequestRepository borrowingRequestRepository,
            IBookBorrowingRequestDetailsRepository borrowingDetailsRepository,
            ILogger<DashboardService> logger)
        {
            _bookRepository = bookRepository;
            _userRepository = userRepository;
            _borrowingRequestRepository = borrowingRequestRepository;
            _borrowingDetailsRepository = borrowingDetailsRepository;
            _logger = logger;
        }
        #region useLater

        //public async Task<DashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        //{
        //    _logger.LogInformation("Fetching dashboard data.");
        //    try
        //    {
        //        var utcNow = DateTime.UtcNow;
        //        var startOfMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        //        var startOfNextMonth = startOfMonth.AddMonths(1);

        //        // --- Tính toán Stats ---
        //        // Các Task có thể chạy song song để tăng tốc độ
        //        var totalBooksTask = _bookRepository.GetAllQueryable().CountAsync(cancellationToken);
        //        var totalUsersTask = _userRepository.GetAllQueryable().CountAsync(u => u.IsActive, cancellationToken); // Chỉ đếm user active
        //        var activeRequestsTask = _borrowingRequestRepository.GetAllQueryable().CountAsync(r => r.Status == BorrowingStatus.Waiting, cancellationToken);
        //        var overdueBooksTask = _borrowingDetailsRepository.GetAllQueryable()
        //                                    .CountAsync(d => d.Request!.Status == BorrowingStatus.Approved && // Phải là request đã duyệt
        //                                                    d.ReturnedDate == null &&       // Chưa trả
        //                                                    d.DueDate < utcNow,             // Đã quá hạn
        //                                                cancellationToken);
        //        var booksThisMonthTask = _bookRepository.GetAllQueryable().CountAsync(b => b.CreatedAt >= startOfMonth && b.CreatedAt < startOfNextMonth, cancellationToken);
        //        var usersThisMonthTask = _userRepository.GetAllQueryable().CountAsync(u => u.CreatedAt >= startOfMonth && u.CreatedAt < startOfNextMonth, cancellationToken);
        //        var requestsThisMonthTask = _borrowingRequestRepository.GetAllQueryable().CountAsync(r => r.DateRequested >= startOfMonth && r.DateRequested < startOfNextMonth, cancellationToken);
        //        var returnedThisMonthTask = _borrowingDetailsRepository.GetAllQueryable().CountAsync(d => d.ReturnedDate >= startOfMonth && d.ReturnedDate < startOfNextMonth, cancellationToken);

        //        // Đợi tất cả các task thống kê hoàn thành
        //        await Task.WhenAll(
        //            totalBooksTask, totalUsersTask, activeRequestsTask, overdueBooksTask,
        //            booksThisMonthTask, usersThisMonthTask, requestsThisMonthTask, returnedThisMonthTask
        //        );

        //        var stats = new DashboardStatsDto
        //        {
        //            TotalBooks = totalBooksTask.Result,
        //            TotalUsers = totalUsersTask.Result,
        //            ActiveRequests = activeRequestsTask.Result,
        //            OverdueBooks = overdueBooksTask.Result,
        //            BooksThisMonth = booksThisMonthTask.Result,
        //            UsersThisMonth = usersThisMonthTask.Result,
        //            RequestsThisMonth = requestsThisMonthTask.Result,
        //            ReturnedThisMonth = returnedThisMonthTask.Result
        //        };

        //        // --- Lấy Recent Requests (Ví dụ: 5 request mới nhất) ---
        //        var recentRequestsQuery = _borrowingRequestRepository.GetAllQueryable()
        //                                    .Include(r => r.Requestor) // Lấy tên user
        //                                    .Include(r => r.Details)   // Cần để Count số sách
        //                                    .OrderByDescending(r => r.DateRequested)
        //                                    .Take(5); // Lấy 5 bản ghi mới nhất

        //        var recentRequestsData = await recentRequestsQuery.ToListAsync(cancellationToken);
        //        var recentRequestsDto = recentRequestsData.Select(r => new RecentRequestDto
        //        {
        //            Id = r.Id,
        //            UserId = r.RequestorID,
        //            UserName = r.Requestor?.FullName ?? r.Requestor?.UserName,
        //            RequestDate = r.DateRequested,
        //            Status = r.Status.ToString(),
        //            BooksCount = r.Details?.Count ?? 0
        //        }).ToList();


        //        // --- Lấy Popular Books (Ví dụ: 5 sách được mượn nhiều nhất) ---
        //        // Query này phức tạp hơn, cần đếm số lượt mượn thành công cho mỗi sách
        //        var popularBooksQuery = _borrowingDetailsRepository.GetAllQueryable()
        //                                    .Where(d => d.Request!.Status == BorrowingStatus.Approved) // Chỉ tính các lượt mượn đã duyệt
        //                                    .GroupBy(d => d.BookID) // Nhóm theo BookID
        //                                    .Select(g => new { BookId = g.Key, BorrowCount = g.Count() }) // Đếm số lượt cho mỗi BookId
        //                                    .OrderByDescending(x => x.BorrowCount) // Sắp xếp giảm dần theo lượt mượn
        //                                    .Take(5); // Lấy top 5

        //        var popularBookStats = await popularBooksQuery.ToListAsync(cancellationToken);
        //        var popularBookIds = popularBookStats.Select(p => p.BookId).ToList();

        //        // Lấy thông tin chi tiết của các sách phổ biến này
        //        var popularBooksData = await _bookRepository.GetAllQueryable()
        //                                    .Where(b => popularBookIds.Contains(b.Id))
        //                                    .ToListAsync(cancellationToken);

        //        // Map sang DTO, kết hợp borrow count
        //        var popularBooksDto = popularBooksData.Select(b =>
        //        {
        //            var stat = popularBookStats.FirstOrDefault(p => p.BookId == b.Id);
        //            return new PopularBookDto
        //            {
        //                Id = b.Id,
        //                Title = b.Title,
        //                Author = b.Author,
        //                BorrowCount = stat?.BorrowCount ?? 0, // Lấy borrow count đã tính
        //                Rating = b.AverageRating, // Lấy rating đã tính sẵn
        //                CoverImageUrl = b.CoverImageUrl
        //            };
        //        }).OrderByDescending(b => b.BorrowCount).ToList(); // Sắp xếp lại lần nữa để đảm bảo thứ tự


        //        // --- Tạo kết quả cuối cùng ---
        //        var dashboardData = new DashboardDto
        //        {
        //            Stats = stats,
        //            RecentRequests = recentRequestsDto,
        //            PopularBooks = popularBooksDto
        //            // Thêm CategoryStats nếu cần
        //        };

        //        _logger.LogInformation("Successfully fetched dashboard data.");
        //        return dashboardData;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching dashboard data.");
        //        throw; // Ném lỗi để Controller xử lý
        //    }
        //} 
        #endregion

        public async Task<DashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching dashboard data.");
            try
            {
                var utcNow = DateTime.UtcNow;
                var startOfMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var startOfNextMonth = startOfMonth.AddMonths(1);

                // --- Tính toán Stats (Thực hiện TUẦN TỰ) ---
                _logger.LogDebug("Calculating dashboard stats...");
                var stats = new DashboardStatsDto();

                stats.TotalBooks = await _bookRepository.GetAllQueryable().CountAsync(cancellationToken);
                stats.TotalUsers = await _userRepository.GetAllQueryable().CountAsync(u => u.IsActive, cancellationToken);
                stats.ActiveRequests = await _borrowingRequestRepository.GetAllQueryable().CountAsync(r => r.Status == BorrowingStatus.Waiting, cancellationToken);
                stats.OverdueBooks = await _borrowingDetailsRepository.GetAllQueryable()
                                            .CountAsync(d => d.Request!.Status == BorrowingStatus.Approved &&
                                                            d.ReturnedDate == null &&
                                                            d.DueDate < utcNow, // << Sửa lại điều kiện DueDate
                                                        cancellationToken);
                stats.BooksThisMonth = await _bookRepository.GetAllQueryable().CountAsync(b => b.CreatedAt >= startOfMonth && b.CreatedAt < startOfNextMonth, cancellationToken);
                stats.UsersThisMonth = await _userRepository.GetAllQueryable().CountAsync(u => u.CreatedAt >= startOfMonth && u.CreatedAt < startOfNextMonth, cancellationToken);
                stats.RequestsThisMonth = await _borrowingRequestRepository.GetAllQueryable().CountAsync(r => r.DateRequested >= startOfMonth && r.DateRequested < startOfNextMonth, cancellationToken);
                stats.ReturnedThisMonth = await _borrowingDetailsRepository.GetAllQueryable().CountAsync(d => d.ReturnedDate >= startOfMonth && d.ReturnedDate < startOfNextMonth, cancellationToken);

                _logger.LogDebug("Dashboard stats calculated: {@Stats}", stats);

                // --- Lấy Recent Requests (Ví dụ: 5 request mới nhất) ---
                _logger.LogDebug("Fetching recent requests...");
                var recentRequestsQuery = _borrowingRequestRepository.GetAllQueryable()
                                            .Include(r => r.Requestor)
                                            .Include(r => r.Details)
                                            .OrderByDescending(r => r.DateRequested)
                                            .Take(5);
                var recentRequestsData = await recentRequestsQuery.ToListAsync(cancellationToken);
                var recentRequestsDto = recentRequestsData.Select(r => new RecentRequestDto
                {
                    Id = r.Id,
                    UserId = r.RequestorID,
                    UserName = r.Requestor?.FullName ?? r.Requestor?.UserName,
                    RequestDate = r.DateRequested,
                    Status = r.Status.ToString(),
                    BooksCount = r.Details?.Count ?? 0
                }).ToList();

                _logger.LogDebug("Fetched {Count} recent requests.", recentRequestsDto.Count);


                // --- Lấy Popular Books (Ví dụ: 5 sách được mượn nhiều nhất) ---
                _logger.LogDebug("Fetching popular books...");
                var popularBookStatsQuery = _borrowingDetailsRepository.GetAllQueryable()
                                            .Where(d => d.Request!.Status == BorrowingStatus.Approved)
                                            .GroupBy(d => d.BookID)
                                            .Select(g => new { BookId = g.Key, BorrowCount = g.Count() })
                                            .OrderByDescending(x => x.BorrowCount)
                                            .Take(5);
                var popularBookStats = await popularBookStatsQuery.ToListAsync(cancellationToken);
                var popularBookIds = popularBookStats.Select(p => p.BookId).ToList();

                List<PopularBookDto> popularBooksDto = new List<PopularBookDto>(); // Khởi tạo list rỗng
                if (popularBookIds.Any()) // Chỉ query sách nếu có ID
                {
                    var popularBooksData = await _bookRepository.GetAllQueryable()
                                                    .Where(b => popularBookIds.Contains(b.Id))
                                                    .ToListAsync(cancellationToken);

                    popularBooksDto = popularBooksData.Select(b =>
                    {
                        var stat = popularBookStats.FirstOrDefault(p => p.BookId == b.Id);

                        return new PopularBookDto
                        {
                            Id = b.Id,
                            Title = b.Title,
                            Author = b.Author,
                            BorrowCount = stat?.BorrowCount ?? 0, // Lấy borrow count đã tính
                            Rating = b.AverageRating, // Lấy rating đã tính sẵn
                            CoverImageUrl = b.CoverImageUrl
                        };
                    }).OrderByDescending(b => b.BorrowCount).ToList();
                }
                _logger.LogDebug("Fetched {Count} popular books.", popularBooksDto.Count);


                // --- Tạo kết quả cuối cùng ---
                var dashboardData = new DashboardDto
                {
                    Stats = stats,
                    RecentRequests = recentRequestsDto,
                    PopularBooks = popularBooksDto
                };

                _logger.LogInformation("Successfully fetched dashboard data.");
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data.");
                throw; // Ném lỗi để Controller xử lý
            }
        }
    }
}
