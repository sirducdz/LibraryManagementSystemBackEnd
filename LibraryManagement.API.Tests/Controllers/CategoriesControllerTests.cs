using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Category;
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
    public class CategoriesControllerTests
    {
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<ILogger<CategoriesController>> _mockLogger;
        private CategoriesController _categoriesController;

        [SetUp]
        public void Setup()
        {
            _mockCategoryService = new Mock<ICategoryService>();
            _mockLogger = new Mock<ILogger<CategoriesController>>();
            _categoriesController = new CategoriesController(_mockCategoryService.Object, _mockLogger.Object);

            // Default HttpContext for non-authorized endpoints or to be overridden
            _categoriesController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        // Helper to simulate authenticated user with roles
        private void SetupUserContext(string userId, IEnumerable<string>? roles = null)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _categoriesController.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region GetAllCategories (IEnumerable) Tests

        [Test]
        public async Task GetAllCategories_IEnumerable_ReturnsOkWithCategories()
        {
            // Arrange
            var expectedCategories = new List<CategoryDto>
            {
                new CategoryDto { Id = 1, Name = "Fiction" },
                new CategoryDto { Id = 2, Name = "Science" }
            };
            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync(It.IsAny<CancellationToken>()))
                                .ReturnsAsync(expectedCategories);

            // Act
            // Calling the first overload (no query parameters)
            var actionResult = await _categoriesController.GetAllCategories(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedCategories = okResult.Value as IEnumerable<CategoryDto>;
            Assert.IsNotNull(returnedCategories);
            Assert.AreEqual(expectedCategories.Count, returnedCategories.Count());
            CollectionAssert.AreEquivalent(expectedCategories, returnedCategories);
        }

        [Test]
        public async Task GetAllCategories_IEnumerable_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new Exception("Database connection error");
            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync(It.IsAny<CancellationToken>()))
                                .ThrowsAsync(exception);

            // Act
            var actionResult = await _categoriesController.GetAllCategories(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(actionResult.Result);
            var objectResult = actionResult.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while retrieving categories.")),
                   exception,
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }
        #endregion

        #region GetCategoryById Tests

        [Test]
        public async Task GetCategoryById_CategoryExists_ReturnsOkWithCategoryDto()
        {
            // Arrange
            int categoryId = 1;
            var expectedCategory = new CategoryDto { Id = categoryId, Name = "Fiction" };
            _mockCategoryService.Setup(s => s.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(expectedCategory);

            // Act
            var actionResult = await _categoriesController.GetCategoryById(categoryId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.AreEqual(expectedCategory, okResult?.Value);
        }

        [Test]
        public async Task GetCategoryById_CategoryNotFound_ReturnsNotFound()
        {
            // Arrange
            int categoryId = 99;
            _mockCategoryService.Setup(s => s.GetCategoryByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((CategoryDto?)null);

            // Act
            var actionResult = await _categoriesController.GetCategoryById(categoryId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(actionResult.Result);
        }
        #endregion

        #region CreateCategory Tests

        [Test]
        public async Task CreateCategory_AdminAndValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var createDto = new CreateCategoryDto { Name = "New Category", Description = "A new one" };
            var createdDto = new CategoryDto { Id = 10, Name = "New Category", Description = "A new one" };
            _mockCategoryService.Setup(s => s.CreateCategoryAsync(createDto, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((true, createdDto, null));

            // Act
            var result = await _categoriesController.CreateCategory(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<CreatedAtActionResult>(result);
            var createdResult = result as CreatedAtActionResult;
            Assert.AreEqual(nameof(CategoriesController.GetCategoryById), createdResult?.ActionName);
            Assert.AreEqual(createdDto.Id, createdResult?.RouteValues?["id"]);
            Assert.AreEqual(createdDto, createdResult?.Value);
        }

        [Test]
        public async Task CreateCategory_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var createDto = new CreateCategoryDto { Name = "Existing Category" };
            _mockCategoryService.Setup(s => s.CreateCategoryAsync(createDto, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((false, null, "Category name already exists."));

            // Act
            var result = await _categoriesController.CreateCategory(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult?.Value);
            // Check message if needed: ((dynamic)badRequestResult.Value).message
        }
        #endregion

        #region UpdateCategory Tests

        [Test]
        public async Task UpdateCategory_AdminAndValidData_ReturnsOkWithUpdatedDto()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int categoryId = 1;
            var updateDto = new UpdateCategoryDto { Name = "Updated Category", Description = "Updated desc" };
            var updatedCategoryDto = new CategoryDto { Id = categoryId, Name = "Updated Category", Description = "Updated desc" };
            _mockCategoryService.Setup(s => s.UpdateCategoryAsync(categoryId, updateDto, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((true, updatedCategoryDto, null));

            // Act
            var result = await _categoriesController.UpdateCategory(categoryId, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedCategoryDto, okResult?.Value);
        }

        [Test]
        public async Task UpdateCategory_CategoryNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int categoryId = 99;
            var updateDto = new UpdateCategoryDto { Name = "Non Existent" };
            _mockCategoryService.Setup(s => s.UpdateCategoryAsync(categoryId, updateDto, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((false, null, "Category not found."));

            // Act
            var result = await _categoriesController.UpdateCategory(categoryId, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }
        #endregion

        #region DeleteCategory Tests

        [Test]
        public async Task DeleteCategory_AdminAndValidId_ReturnsNoContent()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int categoryId = 1;
            _mockCategoryService.Setup(s => s.DeleteCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((true, null));

            // Act
            var result = await _categoriesController.DeleteCategory(categoryId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteCategory_CategoryNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int categoryId = 99;
            _mockCategoryService.Setup(s => s.DeleteCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((false, "Category not found."));

            // Act
            var result = await _categoriesController.DeleteCategory(categoryId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task DeleteCategory_HasAssociatedBooks_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int categoryId = 1;
            _mockCategoryService.Setup(s => s.DeleteCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync((false, "Cannot delete category because it has associated books."));

            // Act
            var result = await _categoriesController.DeleteCategory(categoryId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
        #endregion

        #region GetAllCategories (Paged) Tests

        [Test]
        public async Task GetAllCategories_Paged_ValidParams_ReturnsOkWithPagedResult()
        {
            // Arrange
            var queryParams = new CategoryQueryParameters { Page = 1, PageSize = 5 };
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Paged Category" } };
            var pagedResult = new PagedResult<CategoryDto>(categories, 1, 5, 1);
            _mockCategoryService.Setup(s => s.GetAllCategoriesPaginationAsync(queryParams, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(pagedResult);

            // Act
            // Calling the second overload (with query parameters)
            var actionResult = await _categoriesController.GetAllCategories(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.AreEqual(pagedResult, okResult?.Value);
        }

        [Test]
        public async Task GetAllCategories_Paged_InvalidPageParams_ReturnsBadRequest()
        {
            // Arrange
            var queryParams = new CategoryQueryParameters { Page = 0, PageSize = 10 }; // Invalid Page

            // Act
            var actionResult = await _categoriesController.GetAllCategories(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(actionResult.Result);
        }
        #endregion
    }
}