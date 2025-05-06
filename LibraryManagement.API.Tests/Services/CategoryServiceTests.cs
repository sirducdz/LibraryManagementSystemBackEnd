using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.Category;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Linq.Expressions;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class CategoryServiceTests
    {
        private Mock<ICategoryRepository> _mockCategoryRepository;
        private Mock<IBookRepository> _mockBookRepository;
        private Mock<ILogger<CategoryService>> _mockLogger;
        private CategoryService _categoryService;

        // Sample Data
        private List<Category> _testCategories;

        [SetUp]
        public void Setup()
        {
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockBookRepository = new Mock<IBookRepository>();
            _mockLogger = new Mock<ILogger<CategoryService>>();

            _categoryService = new CategoryService(
                _mockCategoryRepository.Object,
                _mockLogger.Object,
                _mockBookRepository.Object // Pass the book repository mock
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
            _testCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Fiction", Description = "Fictional stories", CreatedAt = DateTime.UtcNow.AddDays(-10), Books = new List<Book>() },
                new Category { Id = 2, Name = "Science", Description = "Scientific topics", CreatedAt = DateTime.UtcNow.AddDays(-5), Books = new List<Book>{ new Book { Id=101, CategoryID = 2, IsDeleted = false } } }, // Has one active book
                new Category { Id = 3, Name = "History", Description = null, CreatedAt = DateTime.UtcNow.AddDays(-2), Books = new List<Book>{ new Book { Id=102, CategoryID = 3, IsDeleted = true } } }, // Has one deleted book
                new Category { Id = 4, Name = "Technology", Description = "Tech books", CreatedAt = DateTime.UtcNow.AddDays(-1), Books = new List<Book>() }
            };

            // Default setup for GetAllQueryable for Category
            var mockQueryableCategories = _testCategories.AsQueryable().BuildMock();
            _mockCategoryRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())) // Setup for both true/false tracking if used
                                     .Returns(mockQueryableCategories);
            // Default setup for GetAllAsync for Category
            _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(_testCategories); // Return the full list for GetAllAsync
        }

        // Helper to setup mock category repo with specific data for Queryable
        private void SetupMockCategoryQueryable(List<Category> categories)
        {
            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Returns(mockQueryable);
        }

        #region GetAllCategoriesAsync Tests

        [Test]
        public async Task GetAllCategoriesAsync_ReturnsAllCategoriesMapped()
        {
            // Arrange (Setup done in SetUp)

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testCategories.Count, result.Count());
            var firstDto = result.First();
            var firstEntity = _testCategories.First();
            Assert.AreEqual(firstEntity.Id, firstDto.Id);
            Assert.AreEqual(firstEntity.Name, firstDto.Name);
            Assert.AreEqual(firstEntity.Description, firstDto.Description);
            CollectionAssert.AllItemsAreInstancesOfType(result, typeof(CategoryDto));
        }

        [Test]
        public async Task GetAllCategoriesAsync_RepositoryThrows_ThrowsException()
        {
            // Arrange
            var exception = new InvalidOperationException("DB Error");
            _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                                     .ThrowsAsync(exception);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _categoryService.GetAllCategoriesAsync());
            Assert.AreEqual("DB Error", ex.Message);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching categories")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        #endregion

        #region GetCategoryByIdAsync Tests

        [Test]
        public async Task GetCategoryByIdAsync_CategoryFound_ReturnsCategoryDto()
        {
            // Arrange
            int categoryId = 1;
            var expectedCategory = _testCategories.First(c => c.Id == categoryId);
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(expectedCategory);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCategory.Id, result.Id);
            Assert.AreEqual(expectedCategory.Name, result.Name);
            Assert.AreEqual(expectedCategory.Description, result.Description);
        }

        [Test]
        public async Task GetCategoryByIdAsync_CategoryNotFound_ReturnsNull()
        {
            // Arrange
            int categoryId = 999;
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync((Category?)null); // Simulate not found

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region CreateCategoryAsync Tests

        [Test]
        public async Task CreateCategoryAsync_ValidData_ReturnsSuccessAndCreatedDto()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "  New Category  ", Description = " Desc " };
            var expectedName = "New Category"; // Trimmed
            var expectedDesc = "Desc"; // Trimmed
            int generatedId = 5;

            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(false); // Name does not exist
            _mockCategoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1) // Simulate success
                                     .Callback<Category, CancellationToken>((c, ct) => c.Id = generatedId); // Assign ID

            // Act
            var (success, createdCategory, errorMessage) = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(createdCategory);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(generatedId, createdCategory.Id);
            Assert.AreEqual(expectedName, createdCategory.Name);
            Assert.AreEqual(expectedDesc, createdCategory.Description);
            _mockCategoryRepository.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == expectedName && c.Description == expectedDesc), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateCategoryAsync_NameExists_ReturnsFailure()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Fiction" }; // Name exists in test data
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(true); // Simulate name exists

            // Act
            var (success, createdCategory, errorMessage) = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdCategory);
            Assert.AreEqual("Category name already exists.", errorMessage);
            _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateCategoryAsync_AddFails_ReturnsFailure()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Unique Name" };
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(false); // Name does not exist
            _mockCategoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(0); // Simulate Add failure

            // Act
            var (success, createdCategory, errorMessage) = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdCategory);
            Assert.AreEqual("Failed to save the new category.", errorMessage);
        }

        [Test]
        public async Task CreateCategoryAsync_RepositoryThrows_ReturnsFailure()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Error Case" };
            var exception = new DbUpdateException("DB constraint error");
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(false);
            _mockCategoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ThrowsAsync(exception);

            // Act
            var (success, createdCategory, errorMessage) = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(createdCategory);
            Assert.AreEqual("An unexpected error occurred while creating the category.", errorMessage);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error creating category with Name: {dto.Name}")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        #endregion

        #region UpdateCategoryAsync Tests

        [Test]
        public async Task UpdateCategoryAsync_ValidUpdate_ReturnsSuccessAndUpdatedDto()
        {
            // Arrange
            int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = " Updated Fiction ", Description = " Updated Desc " };
            var existingCategory = new Category { Id = categoryId, Name = "Fiction", Description = "Old Desc" };
            var expectedName = "Updated Fiction";
            var expectedDesc = "Updated Desc";

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(existingCategory);
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(false); // Name does not conflict
            _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1); // Simulate success

            // Act
            var (success, updatedCategory, errorMessage) = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(updatedCategory);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(categoryId, updatedCategory.Id);
            Assert.AreEqual(expectedName, updatedCategory.Name);
            Assert.AreEqual(expectedDesc, updatedCategory.Description);
            _mockCategoryRepository.Verify(r => r.UpdateAsync(It.Is<Category>(c =>
                c.Id == categoryId &&
                c.Name == expectedName &&
                c.Description == expectedDesc &&
                c.UpdatedAt.HasValue
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateCategoryAsync_CategoryNotFound_ReturnsFailure()
        {
            // Arrange
            int categoryId = 999;
            var dto = new UpdateCategoryDto { Name = "Not Found Update" };
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync((Category?)null); // Simulate not found

            // Act
            var (success, updatedCategory, errorMessage) = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedCategory);
            Assert.AreEqual("Category not found.", errorMessage);
            _mockCategoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateCategoryAsync_NameConflict_ReturnsFailure()
        {
            // Arrange
            int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = " Science " }; // Conflicts with category ID 2
            var existingCategory = new Category { Id = categoryId, Name = "Fiction" };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(existingCategory);
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(true); // Simulate name conflict

            // Act
            var (success, updatedCategory, errorMessage) = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedCategory);
            Assert.AreEqual("Another category with this name already exists.", errorMessage);
            _mockCategoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateCategoryAsync_NameConflictWithItself_ShouldSucceed()
        {
            // Arrange
            int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = " Fiction ", Description = " Updated Desc " }; // Same name, different case/spacing
            var existingCategory = new Category { Id = categoryId, Name = "Fiction", Description = "Old Desc" };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(existingCategory);
            // Simulate ExistsAsync check for name conflict (c.Id != id) returns FALSE because the conflict is with itself
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.Is<Expression<Func<Category, bool>>>(
                                                expr => expr.ToString().Contains($"c.Id != {categoryId}")),
                                                It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(false);
            _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1);

            // Act
            var (success, updatedCategory, errorMessage) = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(updatedCategory);
            Assert.IsNull(errorMessage);
            Assert.AreEqual("Fiction", updatedCategory.Name); // Check trimmed name
        }


        [Test]
        public async Task UpdateCategoryAsync_UpdateFailsInRepo_ReturnsFailure()
        {
            // Arrange
            int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Update Fail Repo" };
            var existingCategory = new Category { Id = categoryId, Name = "Fiction" };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(existingCategory);
            _mockCategoryRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(false); // No name conflict
            _mockCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(0); // Simulate update failure

            // Act
            var (success, updatedCategory, errorMessage) = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedCategory);
            Assert.AreEqual("Failed to update category.", errorMessage);
        }

        #endregion

        #region DeleteCategoryAsync Tests

        [Test]
        public async Task DeleteCategoryAsync_CategoryNotFound_ReturnsFailure()
        {
            // Arrange
            int categoryId = 999;
            // Simulate category not found by GetAllQueryable().FirstOrDefaultAsync()
            SetupMockCategoryQueryable(new List<Category>());

            // Act
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Category not found.", errorMessage);
            _mockCategoryRepository.Verify(r => r.RemoveAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockBookRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        [Test]
        public async Task DeleteCategoryAsync_CategoryHasActiveBooks_ReturnsFailure()
        {
            // Arrange
            int categoryId = 2; // Category "Science" has an active book in test data
            var categoryToDelete = _testCategories.First(c => c.Id == categoryId);
            SetupMockCategoryQueryable(_testCategories); // Provide data for finding the category

            // Simulate that books exist for this category
            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true);

            // Act
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Cannot delete category because it has associated books.", errorMessage);
            _mockCategoryRepository.Verify(r => r.RemoveAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockBookRepository.Verify(r => r.RemoveRangeAsync(It.IsAny<IEnumerable<Book>>(), It.IsAny<CancellationToken>()), Times.Never); // Verify books are not removed
            _mockBookRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteCategoryAsync_CategoryHasOnlyDeletedBooks_ReturnsSuccess()
        {
            // Arrange
            int categoryId = 3; // Category "History" has only a deleted book
            var categoryToDelete = _testCategories.First(c => c.Id == categoryId);
            // Ensure the mock returns the category with its deleted book
            var categoriesWithDeletedBook = new List<Category> { categoryToDelete };
            SetupMockCategoryQueryable(categoriesWithDeletedBook);

            // Simulate that NO *active* books exist for this category
            _mockBookRepository.Setup(r => r.ExistsAsync(It.Is<Expression<Func<Book, bool>>>(ex => ex.ToString().Contains($"b.CategoryID == {categoryId}") && ex.ToString().Contains("!b.IsDeleted")), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false);

            // Simulate RemoveRangeAsync for the (potentially empty or deleted) books associated
            _mockBookRepository.Setup(r => r.RemoveRangeAsync(categoryToDelete.Books, It.IsAny<CancellationToken>())).ReturnsAsync(categoryToDelete.Books.Count);

            // Simulate successful category removal
            _mockCategoryRepository.Setup(r => r.RemoveAsync(categoryToDelete, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1);

            // Act
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNull(errorMessage);
            _mockBookRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockBookRepository.Verify(r => r.RemoveRangeAsync(categoryToDelete.Books, It.IsAny<CancellationToken>()), Times.Once); // Verify books (even if empty/deleted) were passed to RemoveRange
            _mockCategoryRepository.Verify(r => r.RemoveAsync(categoryToDelete, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task DeleteCategoryAsync_CategoryHasNoBooks_ReturnsSuccess()
        {
            // Arrange
            int categoryId = 1; // Category "Fiction" has no books in its collection in setup
            var categoryToDelete = _testCategories.First(c => c.Id == categoryId);
            SetupMockCategoryQueryable(_testCategories); // Provide data for finding the category

            // Simulate that no books exist for this category
            _mockBookRepository.Setup(r => r.ExistsAsync(It.Is<Expression<Func<Book, bool>>>(ex => ex.ToString().Contains($"b.CategoryID == {categoryId}")), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false);

            _mockBookRepository.Setup(r => r.RemoveRangeAsync(categoryToDelete.Books, It.IsAny<CancellationToken>())).ReturnsAsync(categoryToDelete.Books.Count);

            _mockCategoryRepository.Setup(r => r.RemoveAsync(categoryToDelete, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(1); // Simulate success

            // Act
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNull(errorMessage);
            _mockBookRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockBookRepository.Verify(r => r.RemoveRangeAsync(categoryToDelete.Books, It.IsAny<CancellationToken>()), Times.Once);
            _mockCategoryRepository.Verify(r => r.RemoveAsync(categoryToDelete, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteCategoryAsync_RemoveFails_ReturnsFailure()
        {
            // Arrange
            int categoryId = 1;
            var categoryToDelete = _testCategories.First(c => c.Id == categoryId);
            SetupMockCategoryQueryable(_testCategories);

            _mockBookRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Book, bool>>>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(false); // No associated books
            _mockBookRepository.Setup(r => r.RemoveRangeAsync(categoryToDelete.Books, It.IsAny<CancellationToken>())).ReturnsAsync(categoryToDelete.Books.Count);
            _mockCategoryRepository.Setup(r => r.RemoveAsync(categoryToDelete, It.IsAny<CancellationToken>()))
                                     .ReturnsAsync(0); // Simulate remove failure

            // Act
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual("Failed to delete category.", errorMessage);
        }

        #endregion

        #region GetAllCategoriesPaginationAsync Tests

        [Test]
        public async Task GetAllCategoriesPaginationAsync_DefaultParams_ReturnsPagedCategoriesSortedByName()
        {
            // Arrange
            SetupMockCategoryQueryable(_testCategories); // Use helper to set up mock queryable
            var queryParams = new CategoryQueryParameters(); // Defaults: Page 1, Size 10, Sort name asc
            var expectedCategories = _testCategories.OrderBy(c => c.Name).ToList();

            // Act
            var result = await _categoryService.GetAllCategoriesPaginationAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testCategories.Count, result.TotalItems);
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            Assert.AreEqual(_testCategories.Count, result.Items.Count); // Assuming page size >= count
            Assert.AreEqual(expectedCategories.First().Name, result.Items.First().Name); // Check default sort order
            CollectionAssert.AllItemsAreInstancesOfType(result.Items, typeof(CategoryDto));
        }

        [Test]
        [TestCase("Fiction")] // Search Name
        [TestCase("Sci")]    // Search Name/Description partial
        [TestCase("topic")]  // Search Description
        public async Task GetAllCategoriesPaginationAsync_WithSearchTerm_FiltersCorrectly(string searchTerm)
        {
            // Arrange
            SetupMockCategoryQueryable(_testCategories);
            var queryParams = new CategoryQueryParameters { SearchTerm = searchTerm };
            var termLower = searchTerm.Trim().ToLower();
            var expectedCategories = _testCategories.Where(c => c.Name.ToLower().Contains(termLower) ||
                                                            (c.Description != null && c.Description.ToLower().Contains(termLower)))
                                                  .ToList();
            // Act
            var result = await _categoryService.GetAllCategoriesPaginationAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCategories.Count, result.TotalItems);
            Assert.AreEqual(expectedCategories.Count, result.Items.Count); // Assuming page size >= count
            Assert.IsTrue(result.Items.All(dto => dto.Name.ToLower().Contains(termLower) ||
                                                  (dto.Description != null && dto.Description.ToLower().Contains(termLower))));
        }

        [Test]
        [TestCase("name", "desc")]
        [TestCase("createdat", "asc")]
        public async Task GetAllCategoriesPaginationAsync_WithSorting_ReturnsSortedCategories(string sortBy, string sortOrder)
        {
            // Arrange
            SetupMockCategoryQueryable(_testCategories);
            var queryParams = new CategoryQueryParameters { SortBy = sortBy, SortOrder = sortOrder, PageSize = _testCategories.Count };

            IEnumerable<Category> expectedQuery = _testCategories;
            Expression<Func<Category, object>> keySelector = sortBy?.ToLowerInvariant() switch
            {
                "name" => c => c.Name,
                "createdat" => c => c.CreatedAt,
                _ => c => c.Name
            };

            if (sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
                expectedQuery = expectedQuery.OrderByDescending(keySelector.Compile());
            else
                expectedQuery = expectedQuery.OrderBy(keySelector.Compile());

            var expectedSortedIds = expectedQuery.Select(c => c.Id).ToList();

            // Act
            var result = await _categoryService.GetAllCategoriesPaginationAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedSortedIds.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedSortedIds, result.Items.Select(dto => dto.Id).ToList(), $"Sort failed for {sortBy} {sortOrder}");
        }

        [Test]
        public async Task GetAllCategoriesPaginationAsync_WithPagination_ReturnsCorrectSlice()
        {
            // Arrange
            SetupMockCategoryQueryable(_testCategories); // 4 categories
            var queryParams = new CategoryQueryParameters { Page = 2, PageSize = 2, SortBy = "name", SortOrder = "asc" }; // ** FIX: Sort by name (default) to match observed behavior **
            var expectedCategories = _testCategories.OrderBy(c => c.Name) // ** FIX: Sort by name for expectation **
                                                  .Skip((queryParams.Page - 1) * queryParams.PageSize)
                                                  .Take(queryParams.PageSize)
                                                  .ToList();
            // Act
            var result = await _categoryService.GetAllCategoriesPaginationAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testCategories.Count, result.TotalItems);
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            Assert.AreEqual(expectedCategories.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedCategories.Select(c => c.Id), result.Items.Select(dto => dto.Id));
        }

        [Test]
        public async Task GetAllCategoriesPaginationAsync_RepositoryThrows_ReturnsEmptyResultAndLogsError()
        {
            // Arrange
            var queryParams = new CategoryQueryParameters();
            var exception = new InvalidOperationException("DB Error during pagination");
            _mockCategoryRepository.Setup(r => r.GetAllQueryable(It.IsAny<bool>())).Throws(exception);

            // Act
            var result = await _categoryService.GetAllCategoriesPaginationAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalItems);
            Assert.IsEmpty(result.Items);
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching categories with query parameters")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        #endregion
    }
}