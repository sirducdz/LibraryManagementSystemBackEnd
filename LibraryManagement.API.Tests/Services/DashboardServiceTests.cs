using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Implementations;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class DashboardServiceTests
    {
        private Mock<IBookRepository> _mockBookRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IBookBorrowingRequestRepository> _mockBorrowingRequestRepository;
        private Mock<IBookBorrowingRequestDetailsRepository> _mockBorrowingDetailsRepository;
        private Mock<ILogger<DashboardService>> _mockLogger;
        private DashboardService _dashboardService;

        [SetUp]
        public void Setup()
        {
            _mockBookRepository = new Mock<IBookRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockBorrowingRequestRepository = new Mock<IBookBorrowingRequestRepository>();
            _mockBorrowingDetailsRepository = new Mock<IBookBorrowingRequestDetailsRepository>();
            _mockLogger = new Mock<ILogger<DashboardService>>();

            _dashboardService = new DashboardService(
                _mockBookRepository.Object,
                _mockUserRepository.Object,
                _mockBorrowingRequestRepository.Object,
                _mockBorrowingDetailsRepository.Object,
                _mockLogger.Object
            );

            // Default setup for GetAllQueryable to return empty lists to avoid NRE
            _mockBookRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(new List<Book>().AsQueryable().BuildMock());
            _mockUserRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(new List<User>().AsQueryable().BuildMock());
            _mockBorrowingRequestRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(new List<BookBorrowingRequest>().AsQueryable().BuildMock());
            _mockBorrowingDetailsRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(new List<BookBorrowingRequestDetails>().AsQueryable().BuildMock());
        }

        [Test]
        public async Task GetDashboardDataAsync_ValidData_ReturnsCorrectDashboardDto()
        {
            // Arrange
            var utcNow = DateTime.UtcNow;
            var startOfMonth = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // --- Prepare Data for Mocks ---
            var booksForStats = new List<Book> { new Book { Id = 10, CreatedAt = utcNow.AddDays(-40) }, new Book { Id = 11 }, new Book { Id = 12, CreatedAt = startOfMonth.AddDays(1) } };
            var usersForStats = new List<User> { new User { IsActive = true }, new User { IsActive = true }, new User { IsActive = false }, new User { IsActive = true, CreatedAt = startOfMonth.AddDays(2) } };
            var borrowingRequestsForStats = new List<BookBorrowingRequest> {
                new BookBorrowingRequest { Status = BorrowingStatus.Waiting, DateRequested = startOfMonth.AddDays(3) }, // Within current month
                new BookBorrowingRequest { Status = BorrowingStatus.Approved, DateRequested = utcNow.AddDays(-2)}      // Within current month (assuming utcNow is not day 1 or 2)
            };
            var borrowingDetailsForStats = new List<BookBorrowingRequestDetails> {
                new BookBorrowingRequestDetails { Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved }, ReturnedDate = null, DueDate = utcNow.AddDays(-1) }, // Overdue
                new BookBorrowingRequestDetails { Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved }, ReturnedDate = null, DueDate = utcNow.AddDays(5) },  // Active, not overdue
                new BookBorrowingRequestDetails { Request = new BookBorrowingRequest { Status = BorrowingStatus.Returned }, ReturnedDate = startOfMonth.AddDays(4), DueDate = startOfMonth.AddDays(0) } // Returned this month
            };

            var requestor1 = new User { Id = 1, FullName = "John Doe", UserName = "johnd" };
            var recentRequestsDataSource = new List<BookBorrowingRequest>
            {
                new BookBorrowingRequest { Id = 101, RequestorID = requestor1.Id, Requestor = requestor1, DateRequested = utcNow.AddHours(-1), Status = BorrowingStatus.Waiting, Details = new List<BookBorrowingRequestDetails> { new BookBorrowingRequestDetails(), new BookBorrowingRequestDetails()} },
                new BookBorrowingRequest { Id = 100, RequestorID = requestor1.Id, Requestor = requestor1, DateRequested = utcNow.AddHours(-5), Status = BorrowingStatus.Approved, Details = new List<BookBorrowingRequestDetails> { new BookBorrowingRequestDetails()} }
            };

            var book1ForPopular = new Book { Id = 1, Title = "Popular Book 1", Author = "Auth1", AverageRating = 4.5m, CoverImageUrl = "url1" };
            var book2ForPopular = new Book { Id = 2, Title = "Popular Book 2", Author = "Auth2", AverageRating = 4.0m, CoverImageUrl = "url2" };
            var popularDetailsDataSource = new List<BookBorrowingRequestDetails>
            {
                new BookBorrowingRequestDetails { BookID = 1, Book = book1ForPopular, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } },
                new BookBorrowingRequestDetails { BookID = 1, Book = book1ForPopular, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } },
                new BookBorrowingRequestDetails { BookID = 2, Book = book2ForPopular, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } }
            };
            var popularBooksEntitiesSource = new List<Book> { book1ForPopular, book2ForPopular };

            // --- Setup Mocks using SetupSequence for clarity and correctness ---
            _mockBookRepository.SetupSequence(r => r.GetAllQueryable(It.IsAny<bool>()))
                .Returns(booksForStats.AsQueryable().BuildMock())        // For TotalBooks
                .Returns(booksForStats.AsQueryable().BuildMock())        // For BooksThisMonth
                .Returns(popularBooksEntitiesSource.AsQueryable().BuildMock()); // For popularBooksData

            _mockUserRepository.SetupSequence(r => r.GetAllQueryable(It.IsAny<bool>()))
                .Returns(usersForStats.AsQueryable().BuildMock())        // For TotalUsers
                .Returns(usersForStats.AsQueryable().BuildMock());       // For UsersThisMonth

            _mockBorrowingRequestRepository.SetupSequence(r => r.GetAllQueryable(It.IsAny<bool>()))
                .Returns(borrowingRequestsForStats.AsQueryable().BuildMock()) // For ActiveRequests
                .Returns(borrowingRequestsForStats.AsQueryable().BuildMock()) // For RequestsThisMonth
                .Returns(recentRequestsDataSource.AsQueryable().BuildMock());  // For recentRequestsQuery

            _mockBorrowingDetailsRepository.SetupSequence(r => r.GetAllQueryable(It.IsAny<bool>()))
                .Returns(borrowingDetailsForStats.AsQueryable().BuildMock()) // For OverdueBooks
                .Returns(borrowingDetailsForStats.AsQueryable().BuildMock()) // For ReturnedThisMonth
                .Returns(popularDetailsDataSource.AsQueryable().BuildMock()); // For popularBookStatsQuery

            // Act
            var result = await _dashboardService.GetDashboardDataAsync();

            // Assert
            Assert.IsNotNull(result);
            // Stats
            Assert.AreEqual(3, result.Stats.TotalBooks);
            Assert.AreEqual(3, result.Stats.TotalUsers); // Only active users
            Assert.AreEqual(1, result.Stats.ActiveRequests); // Status == Waiting
            Assert.AreEqual(1, result.Stats.OverdueBooks);
            Assert.AreEqual(2, result.Stats.BooksThisMonth);
            Assert.AreEqual(4, result.Stats.UsersThisMonth);
            // ** FIX: Expected RequestsThisMonth should be 2 based on borrowingRequestsForStats **
            Assert.AreEqual(2, result.Stats.RequestsThisMonth);
            Assert.AreEqual(1, result.Stats.ReturnedThisMonth);

            // Recent Requests
            Assert.IsNotNull(result.RecentRequests);
            Assert.AreEqual(2, result.RecentRequests.Count); // Based on recentRequestsDataSource
            Assert.AreEqual(101, result.RecentRequests.First().Id);
            Assert.AreEqual(requestor1.FullName, result.RecentRequests.First().UserName);
            Assert.AreEqual(2, result.RecentRequests.First(r => r.Id == 101).BooksCount);

            // Popular Books
            Assert.IsNotNull(result.PopularBooks);
            Assert.AreEqual(2, result.PopularBooks.Count); // Based on popularBooksEntitiesSource
            var popBook1 = result.PopularBooks.FirstOrDefault(p => p.Id == 1);
            Assert.IsNotNull(popBook1);
            Assert.AreEqual("Popular Book 1", popBook1.Title);
            Assert.AreEqual(2, popBook1.BorrowCount); // Calculated from popularDetailsDataSource
            Assert.AreEqual(4.5m, popBook1.Rating);
        }

        [Test]
        public async Task GetDashboardDataAsync_NoData_ReturnsDtoWithZeroesAndEmptyLists()
        {
            // Arrange
            // Default SetUp already mocks empty lists for all repos

            // Act
            var result = await _dashboardService.GetDashboardDataAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Stats);
            Assert.AreEqual(0, result.Stats.TotalBooks);
            Assert.AreEqual(0, result.Stats.TotalUsers);
            Assert.AreEqual(0, result.Stats.ActiveRequests);
            Assert.AreEqual(0, result.Stats.OverdueBooks);
            Assert.AreEqual(0, result.Stats.BooksThisMonth);
            Assert.AreEqual(0, result.Stats.UsersThisMonth);
            Assert.AreEqual(0, result.Stats.RequestsThisMonth);
            Assert.AreEqual(0, result.Stats.ReturnedThisMonth);

            Assert.IsNotNull(result.RecentRequests);
            Assert.IsEmpty(result.RecentRequests);

            Assert.IsNotNull(result.PopularBooks);
            Assert.IsEmpty(result.PopularBooks);
        }

        [Test]
        public async Task GetDashboardDataAsync_BookRepositoryThrows_ThrowsException()
        {
            // Arrange
            var exception = new InvalidOperationException("Book repo error");
            _mockBookRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Throws(exception);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _dashboardService.GetDashboardDataAsync());
            Assert.AreEqual(exception.Message, ex.Message);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching dashboard data")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Test]
        public async Task GetDashboardDataAsync_UserRepositoryThrows_ThrowsException()
        {
            // Arrange
            var exception = new InvalidOperationException("User repo error");
            // Setup BookRepository to return something to pass the first stat calculation
            _mockBookRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(new List<Book>().AsQueryable().BuildMock());
            _mockUserRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Throws(exception);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _dashboardService.GetDashboardDataAsync());
            Assert.AreEqual(exception.Message, ex.Message);
            _mockLogger.Verify(
              x => x.Log(
                  LogLevel.Error,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching dashboard data")),
                  exception,
                  It.IsAny<Func<It.IsAnyType, Exception, string>>()),
              Times.Once);
        }
        [Test]
        public async Task GetDashboardDataAsync_PopularBooks_HandlesNoBorrowDetails()
        {
            // Arrange
            // All other stats are 0 or from empty lists by default setup
            _mockBorrowingDetailsRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>()))
                                           .Returns(new List<BookBorrowingRequestDetails>().AsQueryable().BuildMock());
            // Act
            var result = await _dashboardService.GetDashboardDataAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.PopularBooks);
            Assert.IsEmpty(result.PopularBooks); // Should be empty if no borrowing details
        }
    }
}