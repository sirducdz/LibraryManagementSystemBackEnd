using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.BookRating;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Services.Implementations;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Linq.Expressions;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class BookRatingServiceTests
    {
        private Mock<IBookRatingRepository> _mockBookRatingRepository;
        private Mock<IBookRepository> _mockBookRepository;
        private Mock<IBookBorrowingRequestRepository> _mockBookBorrowingRequestRepository;
        private Mock<ILogger<BookRatingService>> _mockLogger;
        private BookRatingService _bookRatingService;

        [SetUp]
        public void Setup()
        {
            _mockBookRatingRepository = new Mock<IBookRatingRepository>();
            _mockBookRepository = new Mock<IBookRepository>();
            _mockBookBorrowingRequestRepository = new Mock<IBookBorrowingRequestRepository>();
            _mockLogger = new Mock<ILogger<BookRatingService>>();

            _bookRatingService = new BookRatingService(
                _mockBookRatingRepository.Object,
                _mockBookRepository.Object,
                _mockBookBorrowingRequestRepository.Object,
                _mockLogger.Object
            );

            _mockLogger.Setup(
               x => x.Log(
                   It.IsAny<LogLevel>(),
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()));
        }

        #region AddRatingAsync Tests

        [Test]
        public async Task AddRatingAsync_BookNotFound_ReturnsFailure()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false);

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("Book not found.", errorMessage);
        }

        [Test]
        public async Task AddRatingAsync_UserHasNotBorrowedBook_ReturnsFailure()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(true); // Book exists
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                               .ReturnsAsync(false); // User has not borrowed

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("You can only rate books you have borrowed.", errorMessage);
        }

        [Test]
        public async Task AddRatingAsync_ErrorCheckingBorrowingHistory_ReturnsFailureAndLogsError()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            var exception = new InvalidOperationException("DB error");
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                               .ThrowsAsync(exception);

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("Could not verify borrowing history.", errorMessage);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error checking borrowing history")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task AddRatingAsync_UserAlreadyRatedBook_ReturnsFailure()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                               .ReturnsAsync(true); // User has borrowed
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(new BookRating()); // Existing rating found

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("You have already rated this book.", errorMessage);
        }

        [Test]
        public async Task AddRatingAsync_AddRatingFailsInRepository_ReturnsFailure()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookRating?)null);
            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>())).ReturnsAsync(0); // Add failed

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("Failed to save rating.", errorMessage);
        }

        [Test]
        public async Task AddRatingAsync_UpdateBookRatingSummaryFails_ReturnsFailure()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookRating?)null);
            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>())).ReturnsAsync(1); // Add successful

            _mockBookRatingRepository.Setup(r => r.GetAllRatingsForBookCalculationAsync(ratingDto.BookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(new List<BookRating> { new BookRating { StarRating = 5 } });
            _mockBookRepository.Setup(r => r.GetByIdAsync(ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null); // Book not found for update

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("Failed to update book summary after saving rating.", errorMessage);
        }

        [Test]
        public async Task AddRatingAsync_Successful_ReturnsSuccessAndCreatedRatingDto()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 4, Comment = "Good read!" };
            int userId = 1;
            var bookEntity = new Book { Id = ratingDto.BookId, Title = "Test Book", IsDeleted = false };
            var userEntity = new User { Id = userId, FullName = "Test User" };

            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                               .ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync((BookRating?)null);

            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1)
                                     .Callback<BookRating, CancellationToken>((br, ct) => br.Id = 100);

            _mockBookRatingRepository.Setup(r => r.GetAllRatingsForBookCalculationAsync(ratingDto.BookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(new List<BookRating> { new BookRating { StarRating = ratingDto.StarRating } });
            _mockBookRepository.Setup(r => r.GetByIdAsync(ratingDto.BookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(bookEntity);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1);

            var createdRatingEntity = new BookRating
            {
                Id = 100,
                BookID = ratingDto.BookId,
                Book = bookEntity,
                UserID = userId,
                User = userEntity,
                StarRating = ratingDto.StarRating,
                Comment = ratingDto.Comment,
                RatingDate = DateTime.UtcNow
            };
            var ratingList = new List<BookRating> { createdRatingEntity };
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false))
                                     .Returns(ratingList.AsQueryable().BuildMock());


            // Act
            var (success, createdRatingDto, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(createdRatingDto);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(100, createdRatingDto.Id);
            Assert.AreEqual(ratingDto.BookId, createdRatingDto.BookId);
            Assert.AreEqual(bookEntity.Title, createdRatingDto.BookTitle);
            Assert.AreEqual(userId, createdRatingDto.UserId);
            Assert.AreEqual(userEntity.FullName, createdRatingDto.UserFullName);
            Assert.AreEqual(ratingDto.StarRating, createdRatingDto.StarRating);
            Assert.AreEqual(ratingDto.Comment, createdRatingDto.Comment);

            Assert.AreEqual(1, bookEntity.RatingCount);
            Assert.AreEqual((decimal)ratingDto.StarRating, bookEntity.AverageRating);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.Is<Book>(b => b.Id == ratingDto.BookId && b.RatingCount == 1 && b.AverageRating == (decimal)ratingDto.StarRating), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddRatingAsync_GeneralExceptionDuringAdd_ReturnsFailureAndLogsError()
        {
            // Arrange
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };
            int userId = 1;
            var exception = new InvalidOperationException("DB error during add");

            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookRating?)null);
            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);

            // Act
            var (success, createdRating, errorMessage) = await _bookRatingService.AddRatingAsync(ratingDto, userId);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdRating);
            Assert.AreEqual("An error occurred while adding the rating.", errorMessage);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error adding rating")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        #endregion

        #region GetRatingsForBookAsync Tests

        [Test]
        public async Task GetRatingsForBookAsync_BookHasRatings_ReturnsPagedRatings()
        {
            // Arrange
            int bookId = 1;
            var paginationParams = new PaginationParameters { Page = 1, PageSize = 10 };
            var bookEntity = new Book { Id = bookId, Title = "Science Book" };
            var user1 = new User { Id = 101, FullName = "Reviewer One" };
            var user2 = new User { Id = 102, FullName = "Reviewer Two" };

            var ratingsList = new List<BookRating>
            {
                new BookRating { Id = 1, BookID = bookId, Book = bookEntity, UserID = user1.Id, User = user1, StarRating = 5, Comment = "Excellent!", RatingDate = DateTime.UtcNow.AddDays(-1) },
                new BookRating { Id = 2, BookID = bookId, Book = bookEntity, UserID = user2.Id, User = user2, StarRating = 4, Comment = "Very good.", RatingDate = DateTime.UtcNow.AddDays(-2) }
            };
            var mockQueryableRatings = ratingsList.AsQueryable().BuildMock();
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false))
                                     .Returns(mockQueryableRatings);

            // Act
            var result = await _bookRatingService.GetRatingsForBookAsync(bookId, paginationParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.TotalItems);
            Assert.AreEqual(1, result.Page);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("Excellent!", result.Items.First(r => r.Id == 1).Comment);
            Assert.AreEqual(user1.FullName, result.Items.First(r => r.Id == 1).UserFullName);
            Assert.AreEqual(bookEntity.Title, result.Items.First(r => r.Id == 1).BookTitle);
        }

        [Test]
        public async Task GetRatingsForBookAsync_BookHasNoRatings_ReturnsEmptyPagedResult()
        {
            // Arrange
            int bookId = 2;
            var paginationParams = new PaginationParameters { Page = 1, PageSize = 10 };
            var emptyRatingsList = new List<BookRating>();
            var mockQueryableRatings = emptyRatingsList.AsQueryable().BuildMock();
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false))
                                     .Returns(mockQueryableRatings);

            // Act
            var result = await _bookRatingService.GetRatingsForBookAsync(bookId, paginationParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalItems);
            Assert.IsEmpty(result.Items);
            Assert.AreEqual(0, result.TotalPages);
        }

        [Test]
        public async Task GetRatingsForBookAsync_PaginationWorksCorrectly()
        {
            // Arrange
            int bookId = 3;
            var bookEntity = new Book { Id = bookId, Title = "Paged Book" };
            var user = new User { Id = 1, FullName = "Pager" };
            var ratingsList = new List<BookRating>();
            for (int i = 1; i <= 15; i++)
            {
                ratingsList.Add(new BookRating { Id = i, BookID = bookId, Book = bookEntity, UserID = user.Id, User = user, StarRating = (i % 5) + 1, Comment = $"Comment {i}", RatingDate = DateTime.UtcNow.AddMinutes(-i) });
            }
            var sortedRatingsList = ratingsList.OrderByDescending(r => r.RatingDate).ToList();

            var mockQueryableRatings = sortedRatingsList.AsQueryable().BuildMock();
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false))
                                     .Returns(mockQueryableRatings);

            var paginationParams = new PaginationParameters { Page = 2, PageSize = 5 };

            // Act
            var result = await _bookRatingService.GetRatingsForBookAsync(bookId, paginationParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.TotalItems);
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(5, result.PageSize);
            Assert.AreEqual(3, result.TotalPages);
            Assert.AreEqual(5, result.Items.Count);
            Assert.AreEqual(sortedRatingsList[5].Comment, result.Items.First().Comment);
            Assert.AreEqual(sortedRatingsList[9].Comment, result.Items.Last().Comment);
        }

        [Test]
        public async Task GetRatingsForBookAsync_RepositoryThrowsException_ReturnsEmptyPagedResultAndLogsError()
        {
            // Arrange
            int bookId = 4;
            var paginationParams = new PaginationParameters();
            var exception = new TimeoutException("Database timeout");
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false))
                                     .Throws(exception);

            // Act
            var result = await _bookRatingService.GetRatingsForBookAsync(bookId, paginationParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalItems);
            Assert.IsEmpty(result.Items);
            Assert.AreEqual(paginationParams.Page, result.Page);
            Assert.AreEqual(paginationParams.PageSize, result.PageSize);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error fetching ratings for BookId {bookId}")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        #endregion

        #region UpdateBookRatingSummaryAsync Tests
        [Test]
        public async Task UpdateBookRatingSummaryAsync_CalculatesAverageCorrectly()
        {
            int bookId = 10;
            var bookToUpdate = new Book { Id = bookId, Title = "Book for Avg Test" };
            var ratingsForCalc = new List<BookRating>
            {
                new BookRating { StarRating = 5 },
                new BookRating { StarRating = 4 },
                new BookRating { StarRating = 4 },
                new BookRating { StarRating = 3 },
            };

            _mockBookRatingRepository.Setup(r => r.GetAllRatingsForBookCalculationAsync(bookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(ratingsForCalc);
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(bookToUpdate);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1);

            var ratingDto = new CreateBookRatingDto { BookId = bookId, StarRating = 5 };
            int userId = 1;

            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookRating?)null);
            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false)).Returns(new List<BookRating>().AsQueryable().BuildMock());

            await _bookRatingService.AddRatingAsync(ratingDto, userId);

            Assert.AreEqual(ratingsForCalc.Count, bookToUpdate.RatingCount);
            Assert.AreEqual(4.00m, bookToUpdate.AverageRating);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.Is<Book>(b => b.Id == bookId && b.AverageRating == 4.00m && b.RatingCount == 4), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateBookRatingSummaryAsync_NoRatings_SetsZeroAverageAndCount()
        {
            int bookId = 11;
            var bookToUpdate = new Book { Id = bookId, Title = "Book No Ratings Test" };
            var emptyRatingsForCalc = new List<BookRating>();

            _mockBookRatingRepository.Setup(r => r.GetAllRatingsForBookCalculationAsync(bookId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(emptyRatingsForCalc);
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(bookToUpdate);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1);

            var ratingDto = new CreateBookRatingDto { BookId = bookId, StarRating = 3 };
            int userId = 1;
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookBorrowingRequestRepository.Setup(r => r.UserHasBorrowedBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockBookRatingRepository.Setup(r => r.FindByUserAndBookAsync(userId, ratingDto.BookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookRating?)null);
            _mockBookRatingRepository.Setup(r => r.AddAsync(It.IsAny<BookRating>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockBookRatingRepository.Setup(r => r.GetAllQueryable(false)).Returns(new List<BookRating>().AsQueryable().BuildMock());

            await _bookRatingService.AddRatingAsync(ratingDto, userId);

            Assert.AreEqual(0, bookToUpdate.RatingCount);
            Assert.AreEqual(0.0m, bookToUpdate.AverageRating);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.Is<Book>(b => b.Id == bookId && b.AverageRating == 0.0m && b.RatingCount == 0), It.IsAny<CancellationToken>()), Times.Once);
        }
        #endregion
    }
}
