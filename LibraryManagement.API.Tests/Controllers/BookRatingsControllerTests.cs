using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.BookRating;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LibraryManagement.API.Tests.Controllers
{
    [TestFixture]
    public class BookRatingsControllerTests
    {
        private Mock<IBookRatingService> _mockBookRatingService;
        private Mock<ILogger<BookRatingsController>> _mockLogger;
        private BookRatingsController _bookRatingsController;

        [SetUp]
        public void Setup()
        {
            _mockBookRatingService = new Mock<IBookRatingService>();
            _mockLogger = new Mock<ILogger<BookRatingsController>>();
            _bookRatingsController = new BookRatingsController(_mockBookRatingService.Object, _mockLogger.Object);

            // Default HttpContext for non-authorized endpoints or to be overridden
            _bookRatingsController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // Helper to simulate authenticated user
        private void SetupUserContext(string? userId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _bookRatingsController.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region AddRating Tests

        [Test]
        public async Task AddRating_ValidDataAndUser_ReturnsOkWithCreatedRating()
        {
            // Arrange
            SetupUserContext("1"); // Simulate authenticated user with ID "1"
            var ratingDto = new CreateBookRatingDto { BookId = 101, StarRating = 5, Comment = "Great book!" };
            var createdRatingDto = new BookRatingDto { Id = 1, BookId = 101, UserId = 1, StarRating = 5, Comment = "Great book!" };
            _mockBookRatingService.Setup(s => s.AddRatingAsync(ratingDto, 1, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((true, createdRatingDto, null));

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result); // Controller returns OkObjectResult
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedDto = okResult.Value as BookRatingDto;
            Assert.IsNotNull(returnedDto);
            Assert.AreEqual(createdRatingDto.Id, returnedDto.Id);
            Assert.AreEqual(createdRatingDto.Comment, returnedDto.Comment);
        }

        [Test]
        public async Task AddRating_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext("not-an-integer"); // User ID that won't parse to int
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task AddRating_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext(null); // No NameIdentifier claim
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 5 };

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task AddRating_ServiceReturnsBookNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1");
            var ratingDto = new CreateBookRatingDto { BookId = 999, StarRating = 4 }; // Non-existent book
            _mockBookRatingService.Setup(s => s.AddRatingAsync(ratingDto, 1, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((false, null, "Book not found."));

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task AddRating_ServiceReturnsAlreadyRated_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1");
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 3 };
            _mockBookRatingService.Setup(s => s.AddRatingAsync(ratingDto, 1, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((false, null, "User has already rated this book."));

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task AddRating_ServiceReturnsGenericFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1");
            var ratingDto = new CreateBookRatingDto { BookId = 1, StarRating = 2 };
            _mockBookRatingService.Setup(s => s.AddRatingAsync(ratingDto, 1, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((false, null, "Some other service error."));

            // Act
            var result = await _bookRatingsController.AddRating(ratingDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetRatingsForBook Tests

        [Test]
        public async Task GetRatingsForBook_ValidParams_ReturnsOkWithPagedResult()
        {
            // Arrange
            int bookId = 1;
            var paginationParams = new PaginationParameters { Page = 1, PageSize = 5 };
            var ratings = new List<BookRatingDto> { new BookRatingDto { Id = 1, BookId = bookId, StarRating = 5 } };
            var pagedResult = new PagedResult<BookRatingDto>(ratings, 1, 5, 1);
            _mockBookRatingService.Setup(s => s.GetRatingsForBookAsync(bookId, paginationParams, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(pagedResult);

            // Act
            var actionResult = await _bookRatingsController.GetRatingsForBook(bookId, paginationParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedData = okResult.Value as PagedResult<BookRatingDto>;
            Assert.IsNotNull(returnedData);
            Assert.AreEqual(pagedResult.TotalItems, returnedData.TotalItems);
            Assert.AreEqual(pagedResult.Items.Count, returnedData.Items.Count);
            // Headers check is more involved, typically for integration tests
        }

        [Test]
        [TestCase(0, 10)] // Page <= 0, controller should return BadRequest
        [TestCase(1, 0)]  // PageSize <= 0, DTO corrects PageSize to MaxPageSize, controller's IF is false
        [TestCase(-1, 5)] // Page <= 0, controller should return BadRequest
        public async Task GetRatingsForBook_InvalidPaginationParams_HandlesCorrectly(int page, int pageSize)
        {
            // Arrange
            int bookId = 1;
            var paginationParams = new PaginationParameters { Page = page, PageSize = pageSize };

            // Act
            var actionResult = await _bookRatingsController.GetRatingsForBook(bookId, paginationParams, CancellationToken.None);

            // Assert
            if (page <= 0) // The controller's `if` condition will be met
            {
                Assert.IsInstanceOf<BadRequestObjectResult>(actionResult.Result);
                var badRequestResult = actionResult.Result as BadRequestObjectResult;
                Assert.IsNotNull(badRequestResult?.Value);
                dynamic responseValue = badRequestResult.Value;
                Assert.AreEqual("Page number and page size must be greater than 0.", responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
                _mockBookRatingService.Verify(s => s.GetRatingsForBookAsync(It.IsAny<int>(), It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            else // page > 0, but pageSize might be <= 0 (which DTO corrects)
            {
                // DTO's PageSize setter corrects pageSize if <= 0 to MaxPageSize (50 in user's DTO)
                int correctedPageSize = (pageSize <= 0) ? 50 : ((pageSize > 50) ? 50 : pageSize);
                var dummyRatings = new List<BookRatingDto>();
                var pagedResult = new PagedResult<BookRatingDto>(dummyRatings, page, correctedPageSize, 0);

                _mockBookRatingService.Setup(s => s.GetRatingsForBookAsync(bookId,
                                              It.Is<PaginationParameters>(p => p.Page == page && p.PageSize == correctedPageSize),
                                              It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(pagedResult);

                // Re-Act with potentially corrected params if service was called
                actionResult = await _bookRatingsController.GetRatingsForBook(bookId, paginationParams, CancellationToken.None);

                Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
                var okResult = actionResult.Result as OkObjectResult;
                Assert.IsNotNull(okResult);
                var returnedData = okResult.Value as PagedResult<BookRatingDto>;
                Assert.IsNotNull(returnedData);
                Assert.AreEqual(page, returnedData.Page); // Page was valid
                Assert.AreEqual(correctedPageSize, returnedData.PageSize); // PageSize was corrected by DTO
            }
        }

        [Test]
        public async Task GetRatingsForBook_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            int bookId = 1;
            var paginationParams = new PaginationParameters();
            var exception = new Exception("Database error");
            _mockBookRatingService.Setup(s => s.GetRatingsForBookAsync(bookId, paginationParams, It.IsAny<CancellationToken>()))
                                  .ThrowsAsync(exception);

            // Act
            var actionResult = await _bookRatingsController.GetRatingsForBook(bookId, paginationParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(actionResult.Result);
            var objectResult = actionResult.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error fetching ratings for BookId {bookId}")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        [Test]
        public async Task GetRatingsForBook_NoRatingsFound_ReturnsOkWithEmptyPagedResult()
        {
            // Arrange
            int bookId = 1;
            var paginationParams = new PaginationParameters();
            var emptyPagedResult = new PagedResult<BookRatingDto>(new List<BookRatingDto>(), 1, 10, 0);
            _mockBookRatingService.Setup(s => s.GetRatingsForBookAsync(bookId, paginationParams, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(emptyPagedResult);

            // Act
            var actionResult = await _bookRatingsController.GetRatingsForBook(bookId, paginationParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            var pagedResult = okResult?.Value as PagedResult<BookRatingDto>;
            Assert.IsNotNull(pagedResult);
            Assert.IsEmpty(pagedResult.Items);
            Assert.AreEqual(0, pagedResult.TotalItems);
        }

        #endregion
    }
}