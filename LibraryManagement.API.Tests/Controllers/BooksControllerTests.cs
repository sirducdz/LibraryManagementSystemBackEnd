using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Book;
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
    public class BooksControllerTests
    {
        private Mock<IBookService> _mockBookService;
        private Mock<ILogger<BooksController>> _mockLogger;
        private BooksController _booksController;

        [SetUp]
        public void Setup()
        {
            _mockBookService = new Mock<IBookService>();
            _mockLogger = new Mock<ILogger<BooksController>>();
            _booksController = new BooksController(_mockBookService.Object, _mockLogger.Object);

            // Default HttpContext for non-authorized endpoints or to be overridden
            _booksController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // Helper to simulate authenticated user with roles
        private void SetupUserContext(string userId, IEnumerable<string>? roles = null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _booksController.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region GetBooks Tests

        [Test]
        public async Task GetBooks_ValidParams_ReturnsOkWithPagedResult()
        {
            // Arrange
            var queryParams = new BookQueryParameters { Page = 1, PageSize = 10 };
            var books = new List<BookSummaryDto> { new BookSummaryDto { Id = 1, Title = "Book 1" } };
            var pagedResult = new PagedResult<BookSummaryDto>(books, 1, 10, 1);
            _mockBookService.Setup(s => s.GetBooksAsync(queryParams, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(pagedResult);

            // Act
            var actionResult = await _booksController.GetBooks(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedData = okResult.Value as PagedResult<BookSummaryDto>;
            Assert.IsNotNull(returnedData);
            Assert.AreEqual(pagedResult.TotalItems, returnedData.TotalItems);
            Assert.AreEqual(pagedResult.Items.Count, returnedData.Items.Count);
            // Check for X-Pagination headers (this is harder to test directly without HttpContext mock for Response)
        }

        [Test]
        [TestCase(0, 10)]
        [TestCase(1, 0)]
        [TestCase(-1, 10)]
        [TestCase(1, -5)]
        public async Task GetBooks_InvalidPageOrPageSize_ReturnsBadRequest(int page, int pageSize)
        {
            // Arrange
            var queryParams = new BookQueryParameters { Page = page, PageSize = pageSize };

            // Act
            var actionResult = await _booksController.GetBooks(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(actionResult.Result);
        }

        [Test]
        public async Task GetBooks_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var queryParams = new BookQueryParameters();
            var exception = new Exception("Database error");
            _mockBookService.Setup(s => s.GetBooksAsync(queryParams, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var actionResult = await _booksController.GetBooks(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(actionResult.Result);
            var objectResult = actionResult.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while fetching books")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }
        #endregion

        #region GetBookById Tests

        [Test]
        public async Task GetBookById_BookExists_ReturnsOkWithBookDetailDto()
        {
            // Arrange
            int bookId = 1;
            var bookDetail = new BookDetailDto { Id = bookId, Title = "Existing Book" };
            _mockBookService.Setup(s => s.GetBookByIdAsync(bookId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(bookDetail);

            // Act
            var actionResult = await _booksController.GetBookById(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.AreEqual(bookDetail, okResult?.Value);
        }

        [Test]
        public async Task GetBookById_BookNotFound_ReturnsNotFound()
        {
            // Arrange
            int bookId = 99;
            _mockBookService.Setup(s => s.GetBookByIdAsync(bookId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((BookDetailDto?)null);

            // Act
            var actionResult = await _booksController.GetBookById(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(actionResult.Result);
        }

        [Test]
        public async Task GetBookById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            int bookId = 1;
            var exception = new Exception("Service error");
            _mockBookService.Setup(s => s.GetBookByIdAsync(bookId, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var actionResult = await _booksController.GetBookById(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(actionResult.Result);
            var objectResult = actionResult.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
              x => x.Log(
                  LogLevel.Error,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"An error occurred while fetching book with ID {bookId}")),
                  exception,
                  It.IsAny<Func<It.IsAnyType, Exception, string>>()),
              Times.Once);
        }

        #endregion

        #region CreateBook Tests

        [Test]
        public async Task CreateBook_AdminAndValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var createDto = new CreateBookDto { Title = "New Book", CategoryID = 1, TotalQuantity = 5 };
            var createdBookDto = new BookDetailDto { Id = 10, Title = "New Book" };
            _mockBookService.Setup(s => s.CreateBookAsync(createDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, createdBookDto, null));

            // Act
            var result = await _booksController.CreateBook(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<CreatedAtActionResult>(result);
            var createdResult = result as CreatedAtActionResult;
            Assert.AreEqual(nameof(BooksController.GetBookById), createdResult?.ActionName);
            Assert.AreEqual(createdBookDto.Id, createdResult?.RouteValues?["id"]);
            Assert.AreEqual(createdBookDto, createdResult?.Value);
        }

        [Test]
        public async Task CreateBook_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var createDto = new CreateBookDto { Title = "Conflict Book", CategoryID = 1, TotalQuantity = 1 };
            _mockBookService.Setup(s => s.CreateBookAsync(createDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "ISBN already exists."));

            // Act
            var result = await _booksController.CreateBook(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult?.Value);
            // Check message if needed: ((dynamic)badRequestResult.Value).message
        }

        [Test]
        public async Task CreateBook_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var createDto = new CreateBookDto { Title = "Error Book", CategoryID = 1, TotalQuantity = 1 };
            var exception = new Exception("Create error");
            _mockBookService.Setup(s => s.CreateBookAsync(createDto, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var result = await _booksController.CreateBook(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
        }

        #endregion

        #region UpdateBook Tests

        [Test]
        public async Task UpdateBook_AdminAndValidData_ReturnsNoContent()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 1;
            var updateDto = new UpdateBookDto { Title = "Updated Book", CategoryID = 1, TotalQuantity = 10 };
            _mockBookService.Setup(s => s.UpdateBookAsync(bookId, updateDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, null));

            // Act
            var result = await _booksController.UpdateBook(bookId, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task UpdateBook_BookNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 99;
            var updateDto = new UpdateBookDto { Title = "Non Existent", CategoryID = 1, TotalQuantity = 1 };
            _mockBookService.Setup(s => s.UpdateBookAsync(bookId, updateDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "Book not found."));

            // Act
            var result = await _booksController.UpdateBook(bookId, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task UpdateBook_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 1;
            var updateDto = new UpdateBookDto { Title = "Update Conflict", CategoryID = 1, TotalQuantity = 1, ISBN = "123" };
            _mockBookService.Setup(s => s.UpdateBookAsync(bookId, updateDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "ISBN already exists."));

            // Act
            var result = await _booksController.UpdateBook(bookId, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
        #endregion

        #region DeleteBook Tests

        [Test]
        public async Task DeleteBook_AdminAndValidId_ReturnsNoContent()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 1;
            _mockBookService.Setup(s => s.DeleteBookAsync(bookId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, null));

            // Act
            var result = await _booksController.DeleteBook(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteBook_BookNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 99;
            _mockBookService.Setup(s => s.DeleteBookAsync(bookId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "Book not found."));

            // Act
            var result = await _booksController.DeleteBook(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task DeleteBook_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int bookId = 1;
            _mockBookService.Setup(s => s.DeleteBookAsync(bookId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "Cannot delete book with active borrowings."));

            // Act
            var result = await _booksController.DeleteBook(bookId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
        #endregion
    }
}