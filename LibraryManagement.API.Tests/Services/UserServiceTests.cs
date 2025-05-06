using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Helpers; // Cần cho PasswordHasher
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.DTOs.User;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Implementations;
using LibraryManagement.API.Services.Interfaces; // Cần IUserService để inject nếu test interface
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Linq.Expressions;

namespace LibraryManagement.API.Tests.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        // Mocks for dependencies
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<ILogger<UserService>> _mockLogger;
        private Mock<IUserRefreshTokenRepository> _mockRefreshTokenRepository;
        private Mock<PasswordHasher> _mockPasswordHasher; // Mock cả concrete class nếu cần thiết hoặc tạo instance thật nếu nó đơn giản

        // Service under test
        private IUserService _userService; // Test qua interface


        // Dữ liệu test mẫu cho GetAllUsersAsync
        private List<User> _testUsersList;
        private Role _roleAdmin;
        private Role _roleReader;

        [SetUp]
        public void Setup()
        {
            // Initialize mocks before each test
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockRefreshTokenRepository = new Mock<IUserRefreshTokenRepository>();

            // Nếu PasswordHasher không có dependency phức tạp, có thể dùng instance thật
            // _mockPasswordHasher = new Mock<PasswordHasher>(/* các tham số constructor nếu có */);
            // Hoặc đơn giản là new nếu không có DI phức tạp trong PasswordHasher:
            var realPasswordHasher = new PasswordHasher(); // Giả sử nó không cần DI
            _mockPasswordHasher = new Mock<PasswordHasher>(); // Mock để kiểm soát output nếu cần

            // Setup default behavior for PasswordHasher mock if needed
            _mockPasswordHasher.Setup(ph => ph.HashPassword(It.IsAny<string>())).Returns("hashed_password_mock");
            _mockPasswordHasher.Setup(ph => ph.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);


            // Create the service instance with mocks
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockLogger.Object,
                _mockRefreshTokenRepository.Object,
                _mockPasswordHasher.Object // Sử dụng mock PasswordHasher
            );

            // --- Optional: Setup Logger to swallow logs ---
            // Nếu không muốn kiểm tra log, có thể setup để tránh NullReferenceException nếu logger được dùng nhiều
            _mockLogger.Setup(
               x => x.Log(
                   It.IsAny<LogLevel>(),
                   It.IsAny<EventId>(),
                   It.IsAny<It.IsAnyType>(), // State
                   It.IsAny<Exception>(),
                   (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()));

            // Khởi tạo dữ liệu test cho GetAllUsersAsync
            _roleAdmin = new Role { Id = 1, RoleName = "Administrator" };
            _roleReader = new Role { Id = 2, RoleName = "Reader" };

            _testUsersList = new List<User>
            {
                new User { Id = 1, UserName = "admin", FullName = "Super Admin", Email = "admin@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10), RoleID = _roleAdmin.Id, Role = _roleAdmin, Gender = Gender.Male },
                new User { Id = 2, UserName = "reader1", FullName = "Book Reader One", Email = "reader1@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5), RoleID = _roleReader.Id, Role = _roleReader, Gender = Gender.Female },
                new User { Id = 3, UserName = "inactive", FullName = "Inactive User Account", Email = "inactive@example.com", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-2), RoleID = _roleReader.Id, Role = _roleReader, Gender = Gender.Other },
                new User { Id = 4, UserName = "editor", FullName = "Content Editor", Email = "editor@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20), RoleID = _roleAdmin.Id, Role = _roleAdmin, Gender = Gender.Male },
                new User { Id = 5, UserName = "reader2", FullName = "Another Book Reader", Email = "reader2.test@example.com", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1), RoleID = _roleReader.Id, Role = _roleReader, Gender = Gender.Female }
            };

            // Setup mặc định cho GetAllQueryable
            // Trong service: _userRepository.GetAllQueryable().Include(u => u.Role)
            // Giả định GetAllQueryable() tương đương GetAllQueryable(false)
            var mockQueryableUsers = _testUsersList.AsQueryable().BuildMock();
            _mockUserRepository.Setup(r => r.GetAllQueryable(false)).Returns(mockQueryableUsers);
        }

        // --- Tests for UpdateProfileAsync ---

        [Test]
        public async Task UpdateProfileAsync_UserExistsAndActive_ReturnsSuccessAndUpdatedProfile()
        {
            // Arrange
            int userId = 1;
            var profileDto = new UpdateUserProfileDto { FullName = " New Name ", Gender = Gender.Female };
            var existingUser = new User { Id = userId, FullName = "Old Name", Email = "test@test.com", UserName = "test", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) };
            var expectedUpdatedUser = new User // User entity sau khi update
            {
                Id = userId,
                FullName = "New Name", // Đã Trim()
                Gender = Gender.Female,
                Email = existingUser.Email,
                UserName = existingUser.UserName,
                IsActive = true,
                CreatedAt = existingUser.CreatedAt,
                UpdatedAt = DateTime.UtcNow // Sẽ được gán trong service, kiểm tra sự tồn tại là đủ
            };


            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(1); // Simulate successful update

            // Act
            var (success, updatedProfile, errorMessage) = await _userService.UpdateProfileAsync(userId, profileDto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(updatedProfile);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(userId, updatedProfile?.Id);
            Assert.AreEqual("New Name", updatedProfile?.FullName); // Verify Trimmed value
            Assert.AreEqual(Gender.Female, updatedProfile?.Gender);
            Assert.AreEqual(existingUser.UserName, updatedProfile?.UserName); // Các trường không đổi
            Assert.AreEqual(existingUser.Email, updatedProfile?.Email);

            // Verify that UpdateAsync was called with the correct user data (kiểm tra sâu hơn nếu cần)
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.Is<User>(u =>
               u.Id == userId &&
               u.FullName == "New Name" && // Đã Trim
               u.Gender == Gender.Female &&
               u.UpdatedAt.HasValue // Kiểm tra UpdatedAt đã được set
           ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateProfileAsync_UserNotFound_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            int userId = 99;
            var profileDto = new UpdateUserProfileDto { FullName = "Test" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((User?)null); // Simulate user not found

            // Act
            var (success, updatedProfile, errorMessage) = await _userService.UpdateProfileAsync(userId, profileDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedProfile);
            Assert.AreEqual("User not found or inactive.", errorMessage);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never); // Ensure UpdateAsync wasn't called
        }

        [Test]
        public async Task UpdateProfileAsync_UserInactive_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            int userId = 1;
            var profileDto = new UpdateUserProfileDto { FullName = "Test" };
            var existingUser = new User { Id = userId, FullName = "Old Name", IsActive = false }; // Inactive user

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingUser);

            // Act
            var (success, updatedProfile, errorMessage) = await _userService.UpdateProfileAsync(userId, profileDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedProfile);
            Assert.AreEqual("User not found or inactive.", errorMessage);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateProfileAsync_UpdateFailsInRepository_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            int userId = 1;
            var profileDto = new UpdateUserProfileDto { FullName = " New Name " };
            var existingUser = new User { Id = userId, FullName = "Old Name", IsActive = true };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                               .ReturnsAsync(0); // Simulate update failure (0 rows affected)

            // Act
            var (success, updatedProfile, errorMessage) = await _userService.UpdateProfileAsync(userId, profileDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedProfile);
            Assert.AreEqual("Failed to update profile. Please try again.", errorMessage);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once); // Verify UpdateAsync was called
        }

        [Test]
        public async Task UpdateProfileAsync_RepositoryThrowsException_ReturnsFalseAndGenericErrorMessage()
        {
            // Arrange
            int userId = 1;
            var profileDto = new UpdateUserProfileDto { FullName = " New Name " };
            var exception = new InvalidOperationException("Database error"); // Example exception

            // Simulate GetByIdAsync working fine
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = userId, FullName = "Old Name", IsActive = true });

            // Simulate UpdateAsync throwing an exception
            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                               .ThrowsAsync(exception);

            // Act
            var (success, updatedProfile, errorMessage) = await _userService.UpdateProfileAsync(userId, profileDto);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedProfile);
            Assert.AreEqual("An unexpected error occurred while updating profile.", errorMessage);

            // Verify LogError was called
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => true), // Allow any state object
                   exception, // Check if the correct exception was logged
                   It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
               Times.Once);
        }


        // --- Tests for GetUserProfileByIdAsync ---

        [Test]
        public async Task GetUserProfileByIdAsync_UserExistsAndActive_ReturnsUserProfileDto()
        {
            // Arrange
            int userId = 1;
            var userEntity = new User
            {
                Id = userId,
                UserName = "activeuser",
                Email = "active@test.com",
                FullName = "Active User",
                Gender = Gender.Male,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(userEntity);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result?.Id);
            Assert.AreEqual(userEntity.UserName, result?.UserName);
            Assert.AreEqual(userEntity.Email, result?.Email);
            Assert.AreEqual(userEntity.FullName, result?.FullName);
            Assert.AreEqual(userEntity.Gender, result?.Gender);
            Assert.AreEqual(userEntity.CreatedAt, result?.CreatedAt);
        }

        [Test]
        public async Task GetUserProfileByIdAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            int userId = 99;
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetUserProfileByIdAsync_UserInactive_ReturnsNull()
        {
            // Arrange
            int userId = 1;
            var userEntity = new User { Id = userId, UserName = "inactive", IsActive = false };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(userEntity);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.IsNull(result);
        }

        // --- Tests for CreateUserAsync ---

        [Test]
        public async Task CreateUserAsync_ValidDataAndUserDoesNotExist_ReturnsSuccessAndCreatedUser()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                UserName = "newuser",
                Email = "new@test.com",
                FullName = " New User ",
                Password = "password123",
                RoleID = 2, // Assume RoleID 2 exists
                IsActive = true,
                Gender = Gender.Other
            };

            // Mock ExistsAsync to return false (user doesn't exist)
            _mockUserRepository.Setup(repo => repo.ExistsAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(expr => expr.ToString().Contains("u.UserName == \"newuser\"")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockUserRepository.Setup(repo => repo.ExistsAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(expr => expr.ToString().Contains("u.Email == \"new@test.com\"")), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

            // Mock AddAsync to succeed
            _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(1) // Simulate successful add
                                // Capture the added user to give it an ID for the subsequent fetch
               .Callback<User, CancellationToken>((user, ct) => user.Id = 100); // Assign a dummy ID after Add

            // Mock the fetch after creation (including Role)
            var createdUserEntity = new User
            {
                Id = 100, // The ID assigned in the callback
                UserName = "newuser",
                Email = "new@test.com",
                FullName = "New User", // Service trims this
                PasswordHash = "hashed_password_mock", // From mock hasher
                RoleID = 2,
                IsActive = true,
                Gender = Gender.Other,
                CreatedAt = DateTime.UtcNow, // Will be set by service
                Role = new Role { Id = 2, RoleName = "Member" } // Mock Role for the DTO mapping
            };
            // Mock GetAllQueryable to return a list containing the created user for the final fetch
            var usersList = new List<User> { createdUserEntity }.AsQueryable().BuildMock();

            //var mockUsersQueryable = usersList.AsQueryable().BuildMock(); // << Dùng BuildMock()

            _mockUserRepository.Setup(repo => repo.GetAllQueryable(false))
                              .Returns(usersList); // Mock IQueryable behaviour


            // Act
            var (success, createdUserDtoResult, errorMessage) = await _userService.CreateUserAsync(createUserDto);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(createdUserDtoResult);
            Assert.IsNull(errorMessage);
            Assert.AreEqual(100, createdUserDtoResult?.Id); // Check the assigned ID
            Assert.AreEqual("newuser", createdUserDtoResult?.UserName);
            Assert.AreEqual("new@test.com", createdUserDtoResult?.Email);
            Assert.AreEqual("New User", createdUserDtoResult?.FullName); // Verify Trimmed
            Assert.AreEqual(2, createdUserDtoResult?.RoleID);
            Assert.AreEqual("Member", createdUserDtoResult?.RoleName);
            Assert.IsTrue(createdUserDtoResult?.IsActive);
            Assert.AreEqual(Gender.Other, createdUserDtoResult?.Gender);


            // Verify AddAsync was called with correct data (before ID assigned)
            _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u =>
               u.UserName == "newuser" &&
               u.Email == "new@test.com" &&
               u.FullName == "New User" && // Trimmed
               u.PasswordHash == "hashed_password_mock" &&
               u.RoleID == 2 &&
               u.IsActive == true &&
               u.Gender == Gender.Other
           ), It.IsAny<CancellationToken>()), Times.Once);

            // Verify password hasher was called
            _mockPasswordHasher.Verify(ph => ph.HashPassword("password123"), Times.Once);
        }

        [Test]
        public async Task CreateUserAsync_UserNameExists_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                UserName = "existinguser",
                Email = "new@test.com",
                Password = "password123", // Nên dùng một password có vẻ hợp lệ
                FullName = "Test User",    // Thêm các trường cần thiết để DTO hợp lệ
                RoleID = 1               // Giả sử RoleID 1 là hợp lệ
            };

            // Thiết lập cho validator trả về hợp lệ cho DTO này (đã có trong SetUp,
            // nhưng có thể ghi đè ở đây nếu cần kịch bản validate khác)
            // _mockValidator.Setup(v => v.ValidateAsync(createUserDto, It.IsAny<CancellationToken>()))
            //              .ReturnsAsync(new ValidationResult());


            // Chỉ cần thiết lập cho lời gọi ExistsAsync đầu tiên (kiểm tra UserName) trả về true.
            // Các lời gọi ExistsAsync khác (ví dụ cho Email) sẽ không được thực hiện nếu logic đúng.
            _mockUserRepository.SetupSequence(repo => repo.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Lần gọi đầu tiên (cho UserName "existinguser") -> tồn tại

            // Act
            var (success, createdUser, errorMessage) = await _userService.CreateUserAsync(createUserDto);

            // Assert
            Assert.IsFalse(success, "Success should be false when username exists.");
            Assert.IsNull(createdUser, "CreatedUser should be null when username exists.");
            Assert.AreEqual("Username already exists.", errorMessage, "Error message should indicate username exists.");

            // Verify rằng AddAsync không bao giờ được gọi
            _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

            // Verify rằng ExistsAsync được gọi đúng một lần (chỉ cho việc kiểm tra username)
            _mockUserRepository.Verify(repo => repo.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);

            // (Tùy chọn) Verify PasswordHasher không được gọi
            _mockPasswordHasher.Verify(ph => ph.HashPassword(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CreateUserAsync_EmailExists_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                UserName = "newuser",
                Email = "existing@test.com",
                FullName = "Test User Email Exists",
                Password = "password123", // Đảm bảo password này hợp lệ nếu validator có kiểm tra
                RoleID = 1 // Giả sử RoleID 1 tồn tại
                           // Các thuộc tính khác nếu cần
            };

            // Sử dụng SetupSequence cho các lời gọi ExistsAsync
            // Giả định rằng ExistsAsync cho UserName được gọi TRƯỚC ExistsAsync cho Email trong CreateUserAsync
            _mockUserRepository.SetupSequence(repo => repo.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false) // Lần gọi đầu tiên (cho UserName "newuser") -> không tồn tại
                .ReturnsAsync(true);  // Lần gọi thứ hai (cho Email "existing@test.com") -> tồn tại

            // Act
            var (success, createdUser, errorMessage) = await _userService.CreateUserAsync(createUserDto);

            // Assert
            Assert.IsFalse(success, "Success should be false when email exists.");
            Assert.IsNull(createdUser, "CreatedUser should be null when email exists.");
            Assert.AreEqual("Email already exists.", errorMessage, "Error message should indicate email exists.");

            // Verify rằng AddAsync không bao giờ được gọi
            _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

            // (Tùy chọn) Verify PasswordHasher không được gọi
            _mockPasswordHasher.Verify(ph => ph.HashPassword(It.IsAny<string>()), Times.Never);

            // (Tùy chọn) Verify chính xác các lần gọi ExistsAsync nếu bạn muốn kiểm tra chặt chẽ hơn
            // Moq sẽ tự động kiểm tra thứ tự nếu bạn dùng SetupSequence và các verify sau đó.
            // Nếu bạn muốn verify cụ thể biểu thức nào được gọi, bạn sẽ cần setup riêng biệt
            // và có thể quay lại vấn đề ban đầu nếu dùng ToString().Contains().
            // Với SetupSequence, việc verify Times.Never cho AddAsync thường là đủ để khẳng định logic rẽ nhánh đúng.
            _mockUserRepository.Verify(repo => repo.ExistsAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Kiểm tra ExistsAsync được gọi 2 lần

        }

        // --- Tests for UpdateUserStatusAsync ---

        [Test]
        public async Task UpdateUserStatusAsync_DeactivateUser_ReturnsSuccessAndRevokesTokens()
        {
            // Arrange
            int userId = 5;
            bool newStatus = false; // Deactivating
            var role = new Role { Id = 1, RoleName = "Reader" };
            // Đảm bảo existingUser có đủ thông tin cần thiết cho MapToDto
            var existingUser = new User
            {
                Id = userId,
                UserName = "userToDeactivate",
                Email = "deactivate@test.com", // Thêm email
                FullName = "User To Deactivate", // Thêm FullName
                IsActive = true, // Currently active
                RoleID = role.Id, // Thêm RoleID
                Role = role // Add role for mapping
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Simulate successful update

            _mockRefreshTokenRepository.Setup(repo => repo.RevokeTokensByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Simulate token revocation success

            // Mock the fetch after update
            // Tạo entity user phản ánh đúng trạng thái và thông tin sau khi update
            var updatedUserEntityAfterFetch = new User
            {
                Id = userId,
                UserName = existingUser.UserName,
                Email = existingUser.Email,
                FullName = existingUser.FullName,
                IsActive = false, // Trạng thái đã được cập nhật thành false
                RoleID = role.Id,
                Role = role
            };
            var usersListSource = new List<User> { updatedUserEntityAfterFetch };
            var mockUsersQueryable = usersListSource.AsQueryable().BuildMock(); // << SỬ DỤNG BuildMock()

            _mockUserRepository.Setup(repo => repo.GetAllQueryable(false)) // Đã đúng là false
                               .Returns(mockUsersQueryable); // Trả về mock đã BuildMock()

            // Act
            var (success, updatedUserDto, errorMessage) = await _userService.UpdateUserStatusAsync(userId, newStatus);

            // Assert
            Assert.IsTrue(success, $"UpdateUserStatusAsync returned false. Error: {errorMessage}");
            Assert.IsNotNull(updatedUserDto, "UpdatedUserDto should not be null on success.");
            Assert.IsNull(errorMessage, "ErrorMessage should be null on success.");
            Assert.IsFalse(updatedUserDto?.IsActive, "User status in DTO should be inactive.");
            Assert.AreEqual(userId, updatedUserDto?.Id, "User ID in DTO should match.");

            // Verify UpdateAsync was called with IsActive = false
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.Id == userId && u.IsActive == false), It.IsAny<CancellationToken>()), Times.Once);

            // Verify RevokeTokens was called because user was deactivated
            _mockRefreshTokenRepository.Verify(repo => repo.RevokeTokensByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);

            // Verify các lời gọi quan trọng khác
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetAllQueryable(false), Times.Once);
        }

        [Test]
        public async Task UpdateUserStatusAsync_ActivateUser_ReturnsSuccessAndDoesNotRevokeTokens()
        {
            // Arrange
            int userId = 6;
            bool newStatus = true; // Activating
            var role = new Role { Id = 1, RoleName = "Reader" };
            var existingUser = new User { Id = userId, UserName = "userToActivate", Email = "activate@test.com", FullName = "User To Activate", IsActive = false, RoleID = role.Id, Role = role }; // Currently inactive

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _mockUserRepository.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Simulate successful update count

            // Mock the fetch after update
            // Tạo một entity user phản ánh trạng thái sau khi update
            var updatedUserEntityAfterFetch = new User
            {
                Id = userId,
                UserName = existingUser.UserName,
                Email = existingUser.Email,
                FullName = existingUser.FullName,
                IsActive = true, // Trạng thái đã được cập nhật thành true
                RoleID = role.Id,
                Role = role
            };
            var usersList = new List<User> { updatedUserEntityAfterFetch };
            var mockUsersQueryable = usersList.AsQueryable().BuildMock(); // << Sử dụng BuildMock()

            // Setup GetAllQueryable với trackEntities = false (dựa trên định nghĩa interface của bạn)
            _mockUserRepository.Setup(repo => repo.GetAllQueryable(false)) // << Sửa thành false
                               .Returns(mockUsersQueryable);

            // Act
            var (success, updatedUserDto, errorMessage) = await _userService.UpdateUserStatusAsync(userId, newStatus);

            // Assert
            Assert.IsTrue(success, $"UpdateUserStatusAsync returned false. Error: {errorMessage}");
            Assert.IsNotNull(updatedUserDto, "UpdatedUserDto should not be null on success.");
            Assert.IsNull(errorMessage, "ErrorMessage should be null on success.");
            Assert.IsTrue(updatedUserDto?.IsActive, "User status in DTO should be active.");

            // Verify UpdateAsync was called with IsActive = true
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.Id == userId && u.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);

            // Verify RevokeTokens was NOT called because we are activating, not deactivating
            _mockRefreshTokenRepository.Verify(repo => repo.RevokeTokensByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

            // Verify GetByIdAsync và GetAllQueryable được gọi
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetAllQueryable(false), Times.Once); // Kiểm tra lời gọi fetch sau update
        }
        [Test]
        public async Task UpdateUserStatusAsync_UserNotFound_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            int userId = 99;
            bool newStatus = true;
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((User?)null); // Simulate user not found

            // Act
            var (success, updatedUser, errorMessage) = await _userService.UpdateUserStatusAsync(userId, newStatus);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(updatedUser);
            Assert.AreEqual("User not found.", errorMessage);
            _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockRefreshTokenRepository.Verify(repo => repo.RevokeTokensByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        // TODO: Thêm các test case cho các phương thức khác:
        // - GetAllUsersAsync (kiểm tra filter, sort, pagination)

        // Helper để setup mock repository với một danh sách user cụ thể
        private void SetupMockUserRepositoryWithSpecificData(List<User> usersToReturn)
        {
            var mockQueryable = usersToReturn.AsQueryable().BuildMock();
            _mockUserRepository.Setup(r => r.GetAllQueryable(false)).Returns(mockQueryable);
        }



        #region GetAllUsersAsync Tests

        [Test]
        public async Task GetAllUsersAsync_DefaultParameters_ReturnsAllUsersPagedAndSortedByDefault()
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters(); // Page 1, Size 10, SortBy "username" asc

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testUsersList.Count, result.TotalItems);
            Assert.AreEqual(queryParams.Page, result.Page);
            Assert.AreEqual(queryParams.PageSize, result.PageSize);
            // Mặc định sort theo UserName (hoặc theo định nghĩa trong service của bạn)
            // Service của bạn mặc định sort "username" theo code switch
            var expectedFirstUser = _testUsersList.OrderBy(u => u.UserName).First();
            Assert.AreEqual(expectedFirstUser.UserName, result.Items.First().UserName);
            CollectionAssert.AllItemsAreInstancesOfType(result.Items, typeof(UserDto));
        }

        [Test]
        [TestCase("admin")] // Search UserName
        [TestCase("Book Reader")] // Search FullName
        [TestCase("reader1@example.com")] // Search Email
        public async Task GetAllUsersAsync_WithSearchTerm_FiltersUsersCorrectly(string searchTerm)
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { SearchTerm = searchTerm };
            var termLower = searchTerm.Trim().ToLower();
            var expectedUsers = _testUsersList.Where(u => u.UserName.ToLower().Contains(termLower) ||
                                                         u.FullName.ToLower().Contains(termLower) ||
                                                         u.Email.ToLower().Contains(termLower)).ToList();
            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedUsers.Count, result.TotalItems);
            Assert.AreEqual(expectedUsers.Count, result.Items.Count); // Giả sử page size đủ lớn
            foreach (var item in result.Items)
            {
                Assert.IsTrue(item.UserName.ToLower().Contains(termLower) ||
                              item.FullName.ToLower().Contains(termLower) ||
                              item.Email.ToLower().Contains(termLower));
            }
        }

        [Test]
        public async Task GetAllUsersAsync_WithRoleIdFilter_FiltersUsersByRole()
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { RoleId = _roleAdmin.Id };
            var expectedCount = _testUsersList.Count(u => u.RoleID == _roleAdmin.Id);

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCount, result.TotalItems);
            Assert.IsTrue(result.Items.All(dto => dto.RoleName == _roleAdmin.RoleName));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task GetAllUsersAsync_WithIsActiveFilter_FiltersUsersByStatus(bool isActiveStatus)
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { IsActive = isActiveStatus };
            var expectedCount = _testUsersList.Count(u => u.IsActive == isActiveStatus);

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCount, result.TotalItems);
            Assert.IsTrue(result.Items.All(dto => dto.IsActive == isActiveStatus));
        }

        [Test]
        [TestCase("username", "asc")]
        [TestCase("username", "desc")]
        [TestCase("fullname", "asc")]
        [TestCase("fullname", "desc")]
        [TestCase("email", "asc")]
        [TestCase("email", "desc")]
        [TestCase("role", "asc")] // Sắp xếp theo RoleName
        [TestCase("role", "desc")]
        [TestCase("isactive", "asc")]
        [TestCase("isactive", "desc")]
        [TestCase("createdat", "asc")]
        [TestCase("createdat", "desc")]
        public async Task GetAllUsersAsync_WithSortingOptions_SortsCorrectly(string sortBy, string sortOrder)
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { SortBy = sortBy, SortOrder = sortOrder, PageSize = _testUsersList.Count };

            IOrderedEnumerable<User> expectedSortedUsers;
            Expression<Func<User, object>> keySelector = sortBy?.ToLowerInvariant() switch
            {
                "username" => u => u.UserName,
                "fullname" => u => u.FullName,
                "email" => u => u.Email,
                "role" => u => u.Role!.RoleName, // Chú ý: Nếu Role có thể null, cần xử lý ở đây hoặc đảm bảo test data có Role
                "isactive" => u => u.IsActive,
                "createdat" => u => u.CreatedAt,
                _ => u => u.UserName
            };

            if (sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                expectedSortedUsers = _testUsersList.OrderByDescending(keySelector.Compile());
            }
            else
            {
                expectedSortedUsers = _testUsersList.OrderBy(keySelector.Compile());
            }

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testUsersList.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedSortedUsers.Select(u => u.Id), result.Items.Select(dto => dto.Id), $"Failed for SortBy: {sortBy}, SortOrder: {sortOrder}");
        }

        [Test]
        public async Task GetAllUsersAsync_SortByInvalidColumn_DefaultsToUserNameSortAsc()
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { SortBy = "invalidColumn", SortOrder = "asc", PageSize = _testUsersList.Count };
            var expectedSortedUsers = _testUsersList.OrderBy(u => u.UserName).ToList();

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testUsersList.Count, result.Items.Count);
            CollectionAssert.AreEqual(expectedSortedUsers.Select(u => u.Id), result.Items.Select(dto => dto.Id));
        }

        [Test]
        public async Task GetAllUsersAsync_NoMatchingUsersAfterFilter_ReturnsEmptyItems()
        {
            // Arrange
            SetupMockUserRepositoryWithSpecificData(_testUsersList);
            var queryParams = new UserQueryParameters { SearchTerm = "ThisWillNotMatchAnyUser123XYZ" };

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalItems);
            Assert.IsEmpty(result.Items);
        }

        [Test]
        public async Task GetAllUsersAsync_RepositoryThrowsException_ReturnsEmptyPagedResultAndLogsError()
        {
            // Arrange
            var queryParams = new UserQueryParameters();
            var dbException = new InvalidOperationException("Simulated database connection failed");
            _mockUserRepository.Setup(r => r.GetAllQueryable(false)) // Nhớ là (false)
                               .Throws(dbException);

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalItems, "TotalItems should be 0 on exception.");
            Assert.IsEmpty(result.Items, "Items list should be empty on exception.");
            Assert.AreEqual(queryParams.Page, result.Page, "Page should match queryParams even on exception.");
            Assert.AreEqual(queryParams.PageSize, result.PageSize, "PageSize should match queryParams even on exception.");

            // Verify logger được gọi với Exception
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching all users")),
                    dbException, // Kiểm tra Exception cụ thể đã được log
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_SortByRoleName_HandlesNullRolesGracefullyIfApplicable()
        {
            // Arrange
            var userWithNullRole = new User { Id = 10, UserName = "noroleuser", FullName = "No Role Assigned", Email = "norole@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-3), RoleID = 0, Role = null };
            var usersWithAndWithoutRole = new List<User>(_testUsersList);
            usersWithAndWithoutRole.Add(userWithNullRole);

            SetupMockUserRepositoryWithSpecificData(usersWithAndWithoutRole);

            var queryParams = new UserQueryParameters { SortBy = "role", SortOrder = "asc", PageSize = usersWithAndWithoutRole.Count };

            // Service logic: u => u.Role!.RoleName
            // Nếu Role là null, điều này sẽ gây NullReferenceException TRƯỚC khi CountAsync hoặc ToListAsync.
            // Exception này sẽ được bắt bởi try-catch trong service.

            // Act
            var result = await _userService.GetAllUsersAsync(queryParams);

            // Assert
            // Do u.Role!.RoleName sẽ throw nếu Role null, service sẽ vào catch block
            Assert.IsNotNull(result, "Result should not be null even on exception.");
            Assert.AreEqual(0, result.TotalItems, "TotalItems should be 0 due to exception during query building/execution.");
            Assert.IsEmpty(result.Items, "Items should be empty due to exception.");
            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching all users")),
                   It.IsAny<NullReferenceException>(), // Hoặc ArgumentNullException tùy vào cách EF Core xử lý
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }
        #endregion GetAllUsersAsync Tests
        // - GetUserByIdAsync (Admin view - lấy cả user inactive/deleted?)
        // - UpdateUserAsync (Admin update)
        // - DeleteUserAsync (kiểm tra soft delete/hard delete, revoke token)
        // - Các trường hợp Exception khác
    }
}