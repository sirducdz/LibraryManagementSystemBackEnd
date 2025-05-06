using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Book;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Linq.Expressions;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class BookServiceTests
    {
        private Mock<IBookRepository> _mockBookRepository;
        private Mock<ILogger<BookService>> _mockLogger;
        private BookService _bookService;

        // Sample Data
        private List<Book> _testBooks;
        private Category _categoryFiction;
        private Category _categoryScience;

        [SetUp]
        public void Setup()
        {
            _mockBookRepository = new Mock<IBookRepository>();
            _mockLogger = new Mock<ILogger<BookService>>();

            _bookService = new BookService(
                _mockBookRepository.Object,
                _mockLogger.Object
            );

            // Setup Logger to swallow logs
            _mockLogger.Setup(
               x => x.Log(
                   It.IsAny<LogLevel>(),
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(),
                   It.IsAny<Exception>(),
                   (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()));

            // Initialize Sample Data
            _categoryFiction = new Category { Id = 1, Name = "Fiction" };
            _categoryScience = new Category { Id = 2, Name = "Science" };

            _testBooks = new List<Book>
            {
                new Book {
                    Id = 1, Title = "The Great Novel", Author = "Jane Doe", CategoryID = _categoryFiction.Id, Category = _categoryFiction, TotalQuantity = 5, AverageRating = 4.5m, RatingCount = 10, CreatedAt = DateTime.UtcNow.AddDays(-30), IsDeleted = false, Publisher="Pub A", PublicationYear=2020, ISBN="111",
                    BorrowingDetails = new List<BookBorrowingRequestDetails> {
                        new BookBorrowingRequestDetails { Id = 101, ReturnedDate = null, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } }, // 1 borrowed
                        new BookBorrowingRequestDetails { Id = 102, ReturnedDate = DateTime.UtcNow.AddDays(-1), Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } } // 1 returned
                    } // Available = 5 - 1 = 4
                },
                new Book {
                    Id = 2, Title = "Science Explained", Author = "John Smith", CategoryID = _categoryScience.Id, Category = _categoryScience, TotalQuantity = 3, AverageRating = 4.0m, RatingCount = 5, CreatedAt = DateTime.UtcNow.AddDays(-15), IsDeleted = false, Publisher="Pub B", PublicationYear=2021, ISBN="222",
                    BorrowingDetails = new List<BookBorrowingRequestDetails> {
                        new BookBorrowingRequestDetails { Id = 103, ReturnedDate = null, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } }, // 1 borrowed
                        new BookBorrowingRequestDetails { Id = 104, ReturnedDate = null, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } }, // 1 borrowed
                        new BookBorrowingRequestDetails { Id = 105, ReturnedDate = null, Request = new BookBorrowingRequest { Status = BorrowingStatus.Approved } }  // 1 borrowed
                    } // Available = 3 - 3 = 0
                },
                new Book {
                    Id = 3, Title = "Another Fiction Story", Author = "Alice Brown", CategoryID = _categoryFiction.Id, Category = _categoryFiction, TotalQuantity = 2, AverageRating = 3.5m, RatingCount = 2, CreatedAt = DateTime.UtcNow.AddDays(-5), IsDeleted = false, Publisher="Pub A", PublicationYear=2022, ISBN="333",
                    BorrowingDetails = new List<BookBorrowingRequestDetails>() // No borrowings
                    // Available = 2 - 0 = 2
                },
                 new Book {
                    Id = 4, Title = "Deleted Book", Author = "Deleted Author", CategoryID = _categoryScience.Id, Category = _categoryScience, TotalQuantity = 1, AverageRating = 0m, RatingCount = 0, CreatedAt = DateTime.UtcNow.AddDays(-100), IsDeleted = true, Publisher="Pub C", PublicationYear=2019, ISBN="444"
                    // Available = N/A (deleted)
                },
                  new Book {
                    Id = 5, Title = "Null Author Book", Author = null, CategoryID = _categoryFiction.Id, Category = _categoryFiction, TotalQuantity = 1, AverageRating = 5.0m, RatingCount = 1, CreatedAt = DateTime.UtcNow.AddDays(-2), IsDeleted = false, Publisher="Pub D", PublicationYear=null, ISBN="555"
                    // Available = 1
                }
            };

            // Default setup for GetAllQueryable (returns non-deleted books)
            // Service applies its own IsDeleted filter if needed, but repo might already do it.
            // Assuming repo returns all, and service filters or relies on EF Core global filters.
            // For testing, we provide the full list and let the service logic (or simulated EF Core behavior) filter.
            var mockQueryableBooks = _testBooks.AsQueryable().BuildMock();
            // Assuming service calls GetAllQueryable() -> GetAllQueryable(false)
            _mockBookRepository.Setup(r => r.GetAllQueryable(false)).Returns(mockQueryableBooks);
        }

        // Helper to setup mock repo with specific data for a test
        private void SetupMockRepositoryWithData(List<Book> books)
        {
            var mockQueryable = books.AsQueryable().BuildMock();
            _mockBookRepository.Setup(r => r.GetAllQueryable(false)).Returns(mockQueryable);
        }

        #region GetBooksAsync Tests

        [Test]
        public async Task GetBooksAsync_DefaultParams_ReturnsNonDeletedBooksPagedAndSorted()
        {
            // Arrange
            // ** FIX: Use the filtered list for expectation **
            var expectedBooks = _testBooks.Where(b => !b.IsDeleted).OrderBy(b => b.Title).ToList();
            // Setup mock to return only non-deleted books (done in SetUp and helper)
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters(); // Defaults: Page 1, Size 10, Sort title asc


            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            // ** FIX: Assert against the count of non-deleted books **
            Assert.AreEqual(expectedBooks.Count, result.TotalItems); // This assertion should now pass
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            Assert.AreEqual(expectedBooks.Count, result.Items.Count); // Assuming page size >= count
            Assert.AreEqual(expectedBooks.First().Title, result.Items.First().Title); // Check default sort order
            CollectionAssert.AllItemsAreInstancesOfType(result.Items, typeof(BookSummaryDto));
            // Check if Available/Copies calculation is correct for first item
            var firstBook = expectedBooks.First(); // Use the filtered list
            var firstDto = result.Items.First();
            int expectedBorrowed = firstBook.BorrowingDetails.Count(d => d.Request?.Status == BorrowingStatus.Approved && d.ReturnedDate == null);
            int expectedAvailable = firstBook.TotalQuantity - expectedBorrowed;
            Assert.AreEqual(expectedAvailable > 0, firstDto.Available);
            Assert.AreEqual(expectedAvailable, firstDto.Copies);
        }

        [Test]
        public async Task GetBooksAsync_FilterByCategoryId_ReturnsMatchingBooks()
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { CategoryId = _categoryFiction.Id };
            var expectedBooks = _testBooks.Where(b => !b.IsDeleted && b.CategoryID == _categoryFiction.Id).ToList();

            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedBooks.Count, result.TotalItems);
            Assert.IsTrue(result.Items.All(dto => dto.CategoryId == _categoryFiction.Id));
        }

        [Test]
        [TestCase("Fiction")] // Search Title
        [TestCase("Doe")]    // Search Author
        public async Task GetBooksAsync_FilterBySearchTerm_ReturnsMatchingBooks(string searchTerm)
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { SearchTerm = searchTerm };
            var termLower = searchTerm.Trim().ToLower();
            var expectedBooks = _testBooks.Where(b => !b.IsDeleted &&
                                                (b.Title.ToLower().Contains(termLower) ||
                                                (b.Author != null && b.Author.ToLower().Contains(termLower))))
                                          .ToList();

            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedBooks.Count, result.TotalItems);
            Assert.AreEqual(expectedBooks.Count, result.Items.Count); // Assuming page size >= count
            Assert.IsTrue(result.Items.All(dto => dto.Title.ToLower().Contains(termLower) ||
                                                (dto.Author != null && dto.Author.ToLower().Contains(termLower))));
        }

        [Test]
        public async Task GetBooksAsync_FilterByIsAvailable_True_ReturnsAvailableBooks()
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { IsAvailable = true };
            // Manual calculation based on test data
            var expectedAvailableBooks = _testBooks.Where(b => !b.IsDeleted && (b.TotalQuantity - b.BorrowingDetails.Count(d => d.Request?.Status == BorrowingStatus.Approved && d.ReturnedDate == null)) > 0).ToList();


            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedAvailableBooks.Count, result.TotalItems);
            Assert.IsTrue(result.Items.All(dto => dto.Available == true));
            Assert.IsTrue(result.Items.All(dto => dto.Copies > 0));
        }

        [Test]
        public async Task GetBooksAsync_FilterByIsAvailable_False_ReturnsUnavailableBooks()
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { IsAvailable = false };
            // Manual calculation based on test data
            var expectedUnavailableBooks = _testBooks.Where(b => !b.IsDeleted && (b.TotalQuantity - b.BorrowingDetails.Count(d => d.Request?.Status == BorrowingStatus.Approved && d.ReturnedDate == null)) <= 0).ToList();

            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUnavailableBooks.Count, result.TotalItems);
            Assert.IsTrue(result.Items.All(dto => dto.Available == false));
            Assert.IsTrue(result.Items.All(dto => dto.Copies <= 0));
        }

        [Test]
        [TestCase("rating", "desc")] // Highest rating first
        [TestCase("author", "asc")]  // Author asc (handles null)
        [TestCase("createdat", "desc")] // Newest first
        [TestCase("year", "asc")] // Oldest publication year first (handles null)
        public async Task GetBooksAsync_WithSorting_ReturnsSortedBooks(string sortBy, string sortOrder)
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { SortBy = sortBy, SortOrder = sortOrder, PageSize = _testBooks.Count }; // Get all items for easy sort check

            // Determine expected order manually based on service logic
            IEnumerable<Book> expectedQuery = _testBooks.Where(b => !b.IsDeleted);
            Expression<Func<Book, object>> keySelector = sortBy?.ToLowerInvariant() switch
            {
                "title" => b => b.Title,
                "author" => b => b.Author ?? "", // Match service null handling
                "rating" => b => b.AverageRating,
                "ratingcount" => b => b.RatingCount,
                "createdat" => b => b.CreatedAt,
                "year" => b => b.PublicationYear ?? 0, // Match service null handling
                "id" => b => b.Id,
                _ => b => b.Title
            };

            if (sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
                expectedQuery = expectedQuery.OrderByDescending(keySelector.Compile());
            else
                expectedQuery = expectedQuery.OrderBy(keySelector.Compile());

            var expectedSortedIds = expectedQuery.Select(b => b.Id).ToList();

            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSortedIds.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedSortedIds, result.Items.Select(dto => dto.Id).ToList(), $"Sort failed for {sortBy} {sortOrder}");
        }

        [Test]
        public async Task GetBooksAsync_WithPagination_ReturnsCorrectSlice()
        {
            // Arrange
            SetupMockRepositoryWithData(_testBooks);
            var queryParams = new BookQueryParameters { Page = 2, PageSize = 2, SortBy = "id", SortOrder = "asc" }; // Sort by ID for predictability
            var expectedBooks = _testBooks.Where(b => !b.IsDeleted).OrderBy(b => b.Id)
                                          .Skip((queryParams.Page - 1) * queryParams.PageSize)
                                          .Take(queryParams.PageSize)
                                          .ToList();

            // Act
            var result = await _bookService.GetBooksAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testBooks.Count(b => !b.IsDeleted), result.TotalItems);
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            Assert.AreEqual(expectedBooks.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedBooks.Select(b => b.Id), result.Items.Select(dto => dto.Id));
        }

        [Test]
        public async Task GetBooksAsync_RepositoryThrows_ThrowsException()
        {
            // Arrange
            var queryParams = new BookQueryParameters();
            var exception = new InvalidOperationException("DB Error");
            _mockBookRepository.Setup(r => r.GetAllQueryable(false)).Throws(exception);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _bookService.GetBooksAsync(queryParams));
            Assert.AreEqual("DB Error", ex.Message); // Check if the original exception is thrown
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching books")),
                   exception, // Verify the exact exception is logged
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        #endregion

        #region GetBookByIdAsync Tests

        [Test]
        public async Task GetBookByIdAsync_BookFound_ReturnsBookDetailDto()
        {
            // Arrange
            int bookId = 1;
            var book = _testBooks.First(b => b.Id == bookId);
            // Need to setup GetAllQueryable because GetBookByIdAsync uses it
            SetupMockRepositoryWithData(_testBooks); // Provides the IQueryable source

            // Act
            var result = await _bookService.GetBookByIdAsync(bookId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(book.Id, result.Id);
            Assert.AreEqual(book.Title, result.Title);
            Assert.AreEqual(book.Author, result.Author);
            Assert.AreEqual(book.Category?.Name, result.Category);
            Assert.AreEqual(book.AverageRating, result.AverageRating);
            // Verify calculated available quantity
            int expectedBorrowed = book.BorrowingDetails.Count(d => d.Request?.Status == BorrowingStatus.Approved && d.ReturnedDate == null);
            int expectedAvailable = book.TotalQuantity - expectedBorrowed;
            Assert.AreEqual(expectedAvailable, result.AvailableQuantity);
        }

        [Test]
        public async Task GetBookByIdAsync_BookNotFound_ReturnsNull()
        {
            // Arrange
            int bookId = 999;
            SetupMockRepositoryWithData(_testBooks); // Provide the source data

            // Act
            var result = await _bookService.GetBookByIdAsync(bookId);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetBookByIdAsync_BookIsDeleted_ReturnsNull()
        {
            // Arrange
            int bookId = 4; // ID of the deleted book in test data
            SetupMockRepositoryWithData(_testBooks);
            // Although the service currently doesn't explicitly filter deleted here,
            // the underlying repo or global query filter might. Let's assume it does.
            // If it doesn't, this test might need adjustment or the service needs fixing.

            // Act
            var result = await _bookService.GetBookByIdAsync(bookId);

            // Assert
            // Assuming deleted books are treated as "not found" for detail view
            Assert.IsNull(result);
        }


        [Test]
        public async Task GetBookByIdAsync_RepositoryThrows_ThrowsException()
        {
            // Arrange
            int bookId = 1;
            var exception = new ApplicationException("Repo Error");
            _mockBookRepository.Setup(r => r.GetAllQueryable(false)).Throws(exception);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(async () => await _bookService.GetBookByIdAsync(bookId));
            Assert.AreEqual("Repo Error", ex.Message);
            _mockLogger.Verify(
              x => x.Log(
                  LogLevel.Error,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error fetching book details for ID: {bookId}")),
                  exception,
                  It.IsAny<Func<It.IsAnyType, Exception, string>>()),
              Times.Once);
        }

        #endregion

        #region CreateBookAsync Tests

        [Test]
        public async Task CreateBookAsync_ValidData_ReturnsSuccessAndCreatedBookDto()
        {
            // Arrange
            var bookDto = new CreateBookDto
            {
                Title = "New Test Book",
                Author = "Author New",
                ISBN = "999888777",
                CategoryID = _categoryScience.Id,
                TotalQuantity = 5,
                Publisher = "New Pub",
                PublicationYear = 2024
            };
            int generatedId = 10;

            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false); // ISBN does not exist
            _mockBookRepository.Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1) // Simulate success
                               .Callback<Book, CancellationToken>((b, ct) => b.Id = generatedId); // Assign ID

            // Mock the GetBookByIdAsync call that happens *after* creation
            var createdBookEntity = new Book { Id = generatedId, Title = bookDto.Title, Author = bookDto.Author, ISBN = bookDto.ISBN, CategoryID = bookDto.CategoryID, Category = _categoryScience, TotalQuantity = bookDto.TotalQuantity, Publisher = bookDto.Publisher, PublicationYear = bookDto.PublicationYear, AverageRating = 0, RatingCount = 0, BorrowingDetails = new List<BookBorrowingRequestDetails>() };
            var listForGetById = new List<Book> { createdBookEntity };
            SetupMockRepositoryWithData(listForGetById); // Make GetByIdAsync find the newly created book


            // Act
            var (success, createdBook, errorMessage) = await _bookService.CreateBookAsync(bookDto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(createdBook);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(generatedId, createdBook.Id);
            Assert.AreEqual(bookDto.Title, createdBook.Title);
            Assert.AreEqual(bookDto.Author, createdBook.Author);
            Assert.AreEqual(_categoryScience.Name, createdBook.Category); // Check category name mapping
            Assert.AreEqual(bookDto.TotalQuantity, createdBook.TotalQuantity);
            Assert.AreEqual(bookDto.TotalQuantity, createdBook.AvailableQuantity); // Initially available = total

            _mockBookRepository.Verify(r => r.AddAsync(It.Is<Book>(b => b.Title == bookDto.Title && b.ISBN == bookDto.ISBN), It.IsAny<CancellationToken>()), Times.Once);
            // Verify GetBookByIdAsync was called (indirectly via GetAllQueryable)
            _mockBookRepository.Verify(r => r.GetAllQueryable(false), Times.Once); // Once for ExistsAsync, Once for GetBookByIdAsync after create
        }

        [Test]
        public async Task CreateBookAsync_IsbnExists_ReturnsFailure()
        {
            // Arrange
            var bookDto = new CreateBookDto { Title = "Duplicate ISBN Book", ISBN = "111", CategoryID = 1, TotalQuantity = 1 };
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(true); // Simulate ISBN exists

            // Act
            var (success, createdBook, errorMessage) = await _bookService.CreateBookAsync(bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdBook);
            Assert.AreEqual("ISBN already exists.", errorMessage);
            _mockBookRepository.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateBookAsync_AddFails_ReturnsFailure()
        {
            // Arrange
            var bookDto = new CreateBookDto { Title = "Fail Add Book", ISBN = "777", CategoryID = 1, TotalQuantity = 1 };
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false); // ISBN does not exist
            _mockBookRepository.Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(0); // Simulate Add failure

            // Act
            var (success, createdBook, errorMessage) = await _bookService.CreateBookAsync(bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdBook);
            Assert.AreEqual("Failed to save the new book.", errorMessage);
        }

        #endregion

        #region UpdateBookAsync Tests

        [Test]
        public async Task UpdateBookAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            int bookId = 1;
            var existingBook = _testBooks.First(b => b.Id == bookId);
            var bookDto = new UpdateBookDto
            {
                Title = "Updated Great Novel",
                Author = "Jane Doe Updated",
                ISBN = "111-New",
                CategoryID = _categoryFiction.Id,
                TotalQuantity = 7,
                Publisher = "Pub A Updated",
                PublicationYear = 2021
            };

            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            // Assume new ISBN doesn't exist
            _mockBookRepository.Setup(r => r.ExistsAsync(It.Is<Expression<Func<Book, bool>>>(ex => ex.ToString().Contains(bookDto.ISBN)), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1); // Simulate success

            // Act
            var (success, errorMessage) = await _bookService.UpdateBookAsync(bookId, bookDto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNull(errorMessage);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.Is<Book>(b =>
                b.Id == bookId &&
                b.Title == bookDto.Title &&
                b.Author == bookDto.Author &&
                b.ISBN == bookDto.ISBN &&
                b.CategoryID == bookDto.CategoryID &&
                b.TotalQuantity == bookDto.TotalQuantity &&
                b.UpdatedAt.HasValue // Check if UpdatedAt was set
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateBookAsync_BookNotFound_ReturnsFailure()
        {
            // Arrange
            int bookId = 999;
            var bookDto = new UpdateBookDto { Title = "Update Fail", CategoryID = 1, TotalQuantity = 1 };
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((Book?)null); // Simulate not found

            // Act
            var (success, errorMessage) = await _bookService.UpdateBookAsync(bookId, bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Book not found.", errorMessage);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateBookAsync_BookIsDeleted_ReturnsFailure()
        {
            // Arrange
            int bookId = 4; // ID of the deleted book
            var bookDto = new UpdateBookDto { Title = "Update Deleted Fail", CategoryID = 1, TotalQuantity = 1 };
            var deletedBook = _testBooks.First(b => b.Id == bookId); // IsDeleted = true
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(deletedBook);

            // Act
            var (success, errorMessage) = await _bookService.UpdateBookAsync(bookId, bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Book not found.", errorMessage); // Service treats deleted as not found for update
            _mockBookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Test]
        public async Task UpdateBookAsync_IsbnConflict_ReturnsFailure()
        {
            // Arrange
            int bookIdToUpdate = 1;
            int conflictingBookId = 2;
            var existingBook = _testBooks.First(b => b.Id == bookIdToUpdate);
            var conflictingIsbn = _testBooks.First(b => b.Id == conflictingBookId).ISBN; // "222"
            var bookDto = new UpdateBookDto { Title = "ISBN Conflict", ISBN = conflictingIsbn, CategoryID = 1, TotalQuantity = 1 };

            _mockBookRepository.Setup(r => r.GetByIdAsync(bookIdToUpdate, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            // Simulate ISBN "222" exists for another book (ID != 1)
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

            // Act
            var (success, errorMessage) = await _bookService.UpdateBookAsync(bookIdToUpdate, bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("ISBN already exists for another book.", errorMessage);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateBookAsync_UpdateFailsInRepo_ReturnsFailure()
        {
            // Arrange
            int bookId = 1;
            var existingBook = _testBooks.First(b => b.Id == bookId);
            var bookDto = new UpdateBookDto { Title = "Update Repo Fail", ISBN = "111", CategoryID = 1, TotalQuantity = 1 };

            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false); // No ISBN conflict
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(0); // Simulate update failure

            // Act
            var (success, errorMessage) = await _bookService.UpdateBookAsync(bookId, bookDto);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Failed to update book. It might have been modified by another user.", errorMessage);
        }

        #endregion

        #region DeleteBookAsync Tests

        [Test]
        public async Task DeleteBookAsync_ValidBook_ReturnsSuccess()
        {
            // Arrange
            int bookId = 1;
            var existingBook = _testBooks.First(b => b.Id == bookId && !b.IsDeleted);
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1); // Simulate success

            // Act
            var (success, errorMessage) = await _bookService.DeleteBookAsync(bookId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNull(errorMessage);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.Is<Book>(b => b.Id == bookId && b.IsDeleted == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteBookAsync_BookNotFound_ReturnsFailure()
        {
            // Arrange
            int bookId = 999;
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((Book?)null); // Simulate not found

            // Act
            var (success, errorMessage) = await _bookService.DeleteBookAsync(bookId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Book not found.", errorMessage);
            _mockBookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task DeleteBookAsync_BookAlreadyDeleted_ReturnsFailure()
        {
            // Arrange
            int bookId = 4; // ID of the already deleted book
            var deletedBook = _testBooks.First(b => b.Id == bookId); // IsDeleted = true
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(deletedBook);

            // Act
            var (success, errorMessage) = await _bookService.DeleteBookAsync(bookId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Book not found.", errorMessage); // Service treats already deleted as not found
            _mockBookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task DeleteBookAsync_UpdateFailsInRepo_ReturnsFailure()
        {
            // Arrange
            int bookId = 1;
            var existingBook = _testBooks.First(b => b.Id == bookId && !b.IsDeleted);
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(0); // Simulate update failure

            // Act
            var (success, errorMessage) = await _bookService.DeleteBookAsync(bookId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Failed to delete book.", errorMessage);
        }

        [Test]
        public async Task DeleteBookAsync_RepositoryThrows_ReturnsFailure()
        {
            // Arrange
            int bookId = 1;
            var existingBook = _testBooks.First(b => b.Id == bookId && !b.IsDeleted);
            var exception = new DbUpdateConcurrencyException("Concurrency error");
            _mockBookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingBook);
            _mockBookRepository.Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
                               .ThrowsAsync(exception); // Simulate exception during update

            // Act
            var (success, errorMessage) = await _bookService.DeleteBookAsync(bookId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("An unexpected error occurred while deleting the book.", errorMessage);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error deleting book with ID: {bookId}")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        #endregion
    }
}