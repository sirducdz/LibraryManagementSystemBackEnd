using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.DTOs.User;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LibraryManagement.API.Tests.Controllers
{
    [TestFixture]
    public class UsersControllerTests
    {
        private Mock<IUserService> _mockUserService;
        private Mock<ILogger<UsersController>> _mockLogger;
        private UsersController _usersController;

        [SetUp]
        public void Setup()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();

            _usersController = new UsersController(_mockUserService.Object, _mockLogger.Object);

            // Mock HttpContext and User for authentication/authorization simulation
            // SetupUserContext("1", "RegularUser"); // Default to a regular user
        }

        // Helper method to simulate an authenticated user
        private void SetupUserContext(string userId, string? role = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
                // Add other claims if needed, e.g., username
                // new Claim(ClaimTypes.Name, "testuser")
            };
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _usersController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetMyProfile Tests

        [Test]
        public async Task GetMyProfile_UserAuthenticated_ReturnsOkWithProfile()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString()); // Simulate user with ID 1
            var expectedProfile = new UserProfileDto { Id = userId, UserName = "test", FullName = "Test User" };
            _mockUserService.Setup(s => s.GetUserProfileByIdAsync(userId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(expectedProfile);

            // Act
            var result = await _usersController.GetMyProfile(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProfile = okResult.Value as UserProfileDto;
            Assert.IsNotNull(returnedProfile);
            Assert.AreEqual(expectedProfile.Id, returnedProfile.Id);
            Assert.AreEqual(expectedProfile.FullName, returnedProfile.FullName);
        }

        [Test]
        public async Task GetMyProfile_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext("invalid-id"); // Simulate invalid ID claim

            // Act
            var result = await _usersController.GetMyProfile(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result.Result);
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult?.Value);
            // Optionally check the message if needed
        }

        [Test]
        public async Task GetMyProfile_NoUserIdClaim_ReturnsUnauthorized() // Important edge case
        {
            // Arrange
            // Simulate user without NameIdentifier claim
            var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _usersController.ControllerContext = new ControllerContext { HttpContext = httpContext };


            // Act
            var result = await _usersController.GetMyProfile(CancellationToken.None);

            // Assert
            // It should fail at `User.FindFirstValue(ClaimTypes.NameIdentifier)` returning null, leading to Unauthorized
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result.Result);
        }


        [Test]
        public async Task GetMyProfile_ServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString());
            _mockUserService.Setup(s => s.GetUserProfileByIdAsync(userId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((UserProfileDto?)null); // Simulate service not finding the profile

            // Act
            var result = await _usersController.GetMyProfile(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region UpdateMyProfile Tests

        [Test]
        public async Task UpdateMyProfile_ValidData_ReturnsOkWithUpdatedProfile()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString());
            var profileDto = new UpdateUserProfileDto { FullName = "Updated Name", Gender = Gender.Female };
            var updatedProfile = new UserProfileDto { Id = userId, FullName = "Updated Name", Gender = Gender.Female };
            _mockUserService.Setup(s => s.UpdateProfileAsync(userId, profileDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, updatedProfile, null)); // Simulate success

            // Act
            var result = await _usersController.UpdateMyProfile(profileDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedProfile, okResult?.Value);
        }


        [Test]
        public async Task UpdateMyProfile_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext("invalid-id");
            var profileDto = new UpdateUserProfileDto { FullName = "Valid Name" };

            // Act
            var result = await _usersController.UpdateMyProfile(profileDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task UpdateMyProfile_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString());
            var profileDto = new UpdateUserProfileDto { FullName = "Valid Name" };
            _mockUserService.Setup(s => s.UpdateProfileAsync(userId, profileDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "User not found")); // Simulate not found

            // Act
            var result = await _usersController.UpdateMyProfile(profileDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task UpdateMyProfile_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString());
            var profileDto = new UpdateUserProfileDto { FullName = "Valid Name" };
            _mockUserService.Setup(s => s.UpdateProfileAsync(userId, profileDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "Some update error")); // Simulate general failure

            // Act
            var result = await _usersController.UpdateMyProfile(profileDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task UpdateMyProfile_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            int userId = 1;
            SetupUserContext(userId.ToString());
            var profileDto = new UpdateUserProfileDto { FullName = "Valid Name" };
            var exception = new Exception("Database error");
            _mockUserService.Setup(s => s.UpdateProfileAsync(userId, profileDto, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var result = await _usersController.UpdateMyProfile(profileDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
              x => x.Log(
                  LogLevel.Error,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Unexpected error updating profile for user {userId}")),
                  exception,
                  It.IsAny<Func<It.IsAnyType, Exception, string>>()),
              Times.Once);
        }

        #endregion

        #region GetAllUsers Tests (Admin)

        [Test]
        public async Task GetAllUsers_AdminUser_ReturnsOkWithPagedResult()
        {
            // Arrange
            SetupUserContext("1", "Admin"); // Simulate Admin user
            var queryParams = new UserQueryParameters { Page = 1, PageSize = 5 };
            var users = new List<UserDto> { new UserDto { Id = 1, UserName = "admin" }, new UserDto { Id = 2, UserName = "user" } };
            var pagedResult = new PagedResult<UserDto>(users, 1, 5, 2);
            _mockUserService.Setup(s => s.GetAllUsersAsync(queryParams, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(pagedResult);

            // Act
            var result = await _usersController.GetAllUsers(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.AreEqual(pagedResult, okResult?.Value);
            // Note: Testing headers added by controller is harder in unit tests
        }

        [Test]
        public async Task GetAllUsers_InvalidQueryParams_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            var queryParams = new UserQueryParameters { Page = 0, PageSize = 10 }; // Invalid Page

            // Act
            var result = await _usersController.GetAllUsers(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
        }

        [Test]
        public async Task GetAllUsers_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            var queryParams = new UserQueryParameters();
            var exception = new Exception("Service error");
            _mockUserService.Setup(s => s.GetAllUsersAsync(queryParams, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var result = await _usersController.GetAllUsers(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
        }

        // Note: Testing the [Authorize(Roles = "Admin")] attribute directly is better suited for integration tests.
        // Unit tests assume the request passes authorization checks if the context is set up correctly.

        #endregion

        #region GetUserById Tests (Admin)

        [Test]
        public async Task GetUserById_AdminUserAndUserFound_ReturnsOkWithUserDto()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int targetUserId = 2;
            var userDto = new UserDto { Id = targetUserId, UserName = "target" };
            _mockUserService.Setup(s => s.GetUserByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(userDto);

            // Act
            var result = await _usersController.GetUserById(targetUserId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.AreEqual(userDto, okResult?.Value);
        }

        [Test]
        public async Task GetUserById_AdminUserAndUserNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int targetUserId = 99;
            _mockUserService.Setup(s => s.GetUserByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((UserDto?)null); // Simulate not found

            // Act
            var result = await _usersController.GetUserById(targetUserId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result.Result);
        }

        [Test]
        public async Task GetUserById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int targetUserId = 2;
            var exception = new Exception("Service error");
            _mockUserService.Setup(s => s.GetUserByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var result = await _usersController.GetUserById(targetUserId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
        }

        #endregion

        #region CreateUser Tests (Admin)

        [Test]
        public async Task CreateUser_AdminUserAndValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            var createDto = new CreateUserDto { UserName = "new", Email = "new@test.com", Password = "password", FullName = "New User", RoleID = 2 };
            var createdDto = new UserDto { Id = 10, UserName = "new", Email = "new@test.com", FullName = "New User", RoleID = 2, RoleName = "Member" };
            _mockUserService.Setup(s => s.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, createdDto, null));

            // Act
            var result = await _usersController.CreateUser(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<CreatedAtActionResult>(result);
            var createdResult = result as CreatedAtActionResult;
            Assert.AreEqual(nameof(UsersController.GetUserById), createdResult?.ActionName); // Check action name
            Assert.AreEqual(createdDto.Id, createdResult?.RouteValues?["id"]); // Check route value
            Assert.AreEqual(createdDto, createdResult?.Value); // Check returned object
        }



        [Test]
        public async Task CreateUser_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            var createDto = new CreateUserDto { UserName = "exists", Email = "exists@test.com", Password = "password", FullName = "Exists", RoleID = 1 };
            _mockUserService.Setup(s => s.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "Username already exists.")); // Simulate failure

            // Act
            var result = await _usersController.CreateUser(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            // Optionally check message: Assert.AreEqual("Username already exists.", ((result as BadRequestObjectResult).Value as dynamic).message);
        }

        [Test]
        public async Task CreateUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            var createDto = new CreateUserDto { UserName = "new", Email = "new@test.com", Password = "password", FullName = "New User", RoleID = 2 };
            var exception = new Exception("Service error");
            _mockUserService.Setup(s => s.CreateUserAsync(createDto, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

            // Act
            var result = await _usersController.CreateUser(createDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
        }

        #endregion

        #region UpdateUser Tests (Admin)

        [Test]
        public async Task UpdateUser_AdminUserAndValidData_ReturnsOkWithUpdatedUser()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToUpdate = 2;
            var updateDto = new UpdateUserDto { FullName = "Updated User", RoleID = 1 };
            var updatedUserDto = new UserDto { Id = userIdToUpdate, FullName = "Updated User", RoleID = 1, RoleName = "Admin" };
            _mockUserService.Setup(s => s.UpdateUserAsync(userIdToUpdate, updateDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, updatedUserDto, null));

            // Act
            var result = await _usersController.UpdateUser(userIdToUpdate, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedUserDto, okResult?.Value);
        }



        [Test]
        public async Task UpdateUser_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToUpdate = 99;
            var updateDto = new UpdateUserDto { FullName = "Not Found", RoleID = 1 };
            _mockUserService.Setup(s => s.UpdateUserAsync(userIdToUpdate, updateDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "User not found"));

            // Act
            var result = await _usersController.UpdateUser(userIdToUpdate, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task UpdateUser_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToUpdate = 2;
            var updateDto = new UpdateUserDto { FullName = "Fail Update", RoleID = 99 }; // Invalid RoleID?
            _mockUserService.Setup(s => s.UpdateUserAsync(userIdToUpdate, updateDto, It.IsAny<CancellationToken>()))
                           .ReturnsAsync((false, null, "Invalid RoleID"));

            // Act
            var result = await _usersController.UpdateUser(userIdToUpdate, updateDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        #endregion

        #region UpdateUserStatus Tests (Admin)

        [Test]
        public async Task UpdateUserStatus_ValidData_ReturnsOkWithUpdatedUser()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToUpdate = 2;
            var statusDto = new UpdateUserStatusDto { IsActive = false };
            var updatedUserDto = new UserDto { Id = userIdToUpdate, IsActive = false, FullName = "Test" }; // Example
            _mockUserService.Setup(s => s.UpdateUserStatusAsync(userIdToUpdate, statusDto.IsActive, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, updatedUserDto, null));

            // Act
            var result = await _usersController.UpdateUserStatus(userIdToUpdate, statusDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedUserDto, okResult?.Value);
            Assert.IsFalse((okResult?.Value as UserDto)?.IsActive);
        }



        [Test]
        public async Task UpdateUserStatus_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToUpdate = 99;
            var statusDto = new UpdateUserStatusDto { IsActive = false };
            _mockUserService.Setup(s => s.UpdateUserStatusAsync(userIdToUpdate, statusDto.IsActive, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, null, "User not found"));

            // Act
            var result = await _usersController.UpdateUserStatus(userIdToUpdate, statusDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        #endregion

        #region DeleteUser Tests (Admin)

        [Test]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            // Arrange
            SetupUserContext("1", "Admin"); // Admin user
            int userIdToDelete = 2;
            _mockUserService.Setup(s => s.DeleteUserAsync(userIdToDelete, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((true, null)); // Simulate success

            // Act
            var result = await _usersController.DeleteUser(userIdToDelete, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteUser_AttemptToDeleteSelf_ReturnsBadRequest()
        {
            // Arrange
            int adminUserId = 1;
            SetupUserContext(adminUserId.ToString(), "Admin"); // Admin user trying to delete self

            // Act
            var result = await _usersController.DeleteUser(adminUserId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            // Optionally check message
        }


        [Test]
        public async Task DeleteUser_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToDelete = 99;
            _mockUserService.Setup(s => s.DeleteUserAsync(userIdToDelete, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "User not found")); // Simulate not found

            // Act
            var result = await _usersController.DeleteUser(userIdToDelete, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task DeleteUser_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToDelete = 2;
            _mockUserService.Setup(s => s.DeleteUserAsync(userIdToDelete, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((false, "Cannot delete due to dependencies")); // Simulate failure

            // Act
            var result = await _usersController.DeleteUser(userIdToDelete, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            // Optionally check message
        }

        [Test]
        public async Task DeleteUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupUserContext("1", "Admin");
            int userIdToDelete = 2;
            var exception = new Exception("Service error");
            _mockUserService.Setup(s => s.DeleteUserAsync(userIdToDelete, It.IsAny<CancellationToken>()))
                           .ThrowsAsync(exception);

            // Act
            var result = await _usersController.DeleteUser(userIdToDelete, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
        }

        #endregion
    }
}