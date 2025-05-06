
using FluentValidation;
using FluentValidation.Results;
using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Auth;
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
    public class AuthControllerTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ILogger<AuthController>> _mockLogger;
        private Mock<IValidator<RegisterRequestDto>> _mockRegisterValidator; // Mock the validator
        private AuthController _authController;

        [SetUp]
        public void Setup()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockRegisterValidator = new Mock<IValidator<RegisterRequestDto>>(); // Instantiate the mock

            // Setup default validation result (valid) - tests can override this
            // ** FIX: Use constructor with empty list of failures for a valid result **
            _mockRegisterValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterRequestDto>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(new ValidationResult(new List<ValidationFailure>())); // Default to valid

            _authController = new AuthController(
                _mockAuthService.Object,
                _mockLogger.Object,
                _mockRegisterValidator.Object // Pass the mock validator
            );

            // Mock HttpContext and User if needed for authenticated endpoints
        }

        // Helper method to simulate an authenticated user (needed for Logout, GetMyInfo)
        private void SetupUserContext(string userId, string? email = null, string? fullName = null, List<string>? roles = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            if (!string.IsNullOrEmpty(email)) claims.Add(new Claim(ClaimTypes.Email, email));
            if (!string.IsNullOrEmpty(fullName)) claims.Add(new Claim(ClaimTypes.Name, fullName));
            if (roles != null) claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region Register Tests

        [Test]
        public async Task Register_ValidData_ReturnsOk()
        {
            // Arrange
            var registerDto = new RegisterRequestDto { Email = "test@example.com", Password = "password", ConfirmPassword = "password", FullName = "Test User", UserName = "testuser", Gender = Gender.Male };
            // ** FIX: Use string for UserId in service result tuple **
            var serviceResult = (Success: true, UserId: "1", ErrorMessage: (string?)null); // Simulate success with string UserId
            // ** FIX: Setup mock with matching tuple type (bool, string, string) **
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult); // No explicit cast needed if types match
                                                          // Simulate valid model state (FluentValidation passed)
            _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new ValidationResult(new List<ValidationFailure>())); // Explicitly valid


            // Act
            var result = await _authController.Register(registerDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult?.Value);
            // Check returned message and userId
            dynamic responseValue = okResult.Value;
            Assert.AreEqual("Registration successful", responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
            // ** FIX: Compare UserId as string **
            Assert.AreEqual(serviceResult.UserId, responseValue.GetType().GetProperty("userId").GetValue(responseValue, null).ToString());
        }


        [Test]
        public async Task Register_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterRequestDto { Email = "exists@example.com", Password = "password", ConfirmPassword = "password", FullName = "Test User", UserName = "testuser" };
            // ** FIX: Use string for UserId in service result tuple **
            var serviceResult = (Success: false, UserId: (string?)null, ErrorMessage: "Email already exists."); // Simulate failure
                                                                                                                // ** FIX: Setup mock with matching tuple type (bool, string, string) **
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);
            _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new ValidationResult(new List<ValidationFailure>())); // Model is valid

            // Act
            var result = await _authController.Register(registerDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult?.Value);
            // Check returned message
            dynamic responseValue = badRequestResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
        }

        [Test]
        public async Task Register_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterRequestDto { Email = "invalid-email" };
            var validationFailure = new ValidationFailure("Email", "Invalid email format");
            // Simulate FluentValidation failing
            _mockRegisterValidator.Setup(v => v.ValidateAsync(registerDto, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(new ValidationResult(new List<ValidationFailure> { validationFailure }));
            // Manually add model error to simulate framework behavior if needed,
            // but for this controller, the service call happens regardless of explicit ModelState check.
            // _authController.ModelState.AddModelError("Email", "Invalid email format");

            // Service should be mocked to return failure because the controller will call it.
            var serviceResult = (Success: false, UserId: (string?)null, ErrorMessage: "Validation failed due to invalid input.");
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.Register(registerDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult?.Value);
            dynamic responseValue = badRequestResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));

            // Verify service was called once
            _mockAuthService.Verify(s => s.RegisterAsync(registerDto, It.IsAny<CancellationToken>()), Times.Once);
        }


        #endregion

        #region Login Tests

        [Test]
        public async Task Login_ValidCredentials_ReturnsOkWithTokens()
        {
            // Arrange
            var loginDto = new LoginRequestDto { UserName = "testuser", Password = "password" };
            var tokens = new TokenResponseDto { AccessToken = "access_token", RefreshToken = "refresh_token", AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15) };
            var serviceResult = (Success: true, Tokens: tokens, ErrorMessage: (string?)null);
            _mockAuthService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.Login(loginDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(tokens, okResult?.Value);
        }

        [Test]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginRequestDto { UserName = "testuser", Password = "wrongpassword" };
            var serviceResult = (Success: false, Tokens: (TokenResponseDto?)null, ErrorMessage: "Invalid credentials.");
            _mockAuthService.Setup(s => s.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.Login(loginDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult?.Value);
            dynamic responseValue = unauthorizedResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
        }
        #endregion

        #region RefreshToken Tests

        [Test]
        public async Task RefreshToken_ValidToken_ReturnsOkWithNewTokens()
        {
            // Arrange
            var tokenRequest = new TokenRequestDto { RefreshToken = "valid_refresh_token", AccessToken = "old_access_token" };
            var newTokens = new TokenResponseDto { AccessToken = "new_access", RefreshToken = "new_refresh", AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15) };
            // ** FIX: Use ValueTuple for service result **
            var serviceResult = (Success: true, Tokens: newTokens, ErrorMessage: (string?)null);
            // ** FIX: Setup mock with matching tuple type **
            _mockAuthService.Setup(s => s.RefreshTokenAsync(tokenRequest, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.RefreshToken(tokenRequest, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(newTokens, okResult?.Value);
        }

        [Test]
        public async Task RefreshToken_MissingRefreshToken_ReturnsBadRequest()
        {
            // Arrange
            var tokenRequest = new TokenRequestDto { RefreshToken = "", AccessToken = "old_access" }; // Empty RefreshToken

            // Act
            var result = await _authController.RefreshToken(tokenRequest, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            // Verify service was not called
            _mockAuthService.Verify(s => s.RefreshTokenAsync(It.IsAny<TokenRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task RefreshToken_NullRefreshTokenInRequest_ReturnsBadRequest()
        {
            // Arrange
            var requestWithNullToken = new TokenRequestDto { RefreshToken = null, AccessToken = "old_access" }; // Null RefreshToken

            // Act
            var result = await _authController.RefreshToken(requestWithNullToken, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            _mockAuthService.Verify(s => s.RefreshTokenAsync(It.IsAny<TokenRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Test]
        public async Task RefreshToken_ServiceReturnsFailure_ReturnsUnauthorized()
        {
            // Arrange
            var tokenRequest = new TokenRequestDto { RefreshToken = "invalid_or_expired_token", AccessToken = "old_access" };
            // ** FIX: Use ValueTuple for service result **
            var serviceResult = (Success: false, Tokens: (TokenResponseDto?)null, ErrorMessage: "Invalid refresh token.");
            // ** FIX: Setup mock with matching tuple type **
            _mockAuthService.Setup(s => s.RefreshTokenAsync(tokenRequest, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.RefreshToken(tokenRequest, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult?.Value);
            dynamic responseValue = unauthorizedResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
        }

        #endregion

        #region Logout Tests

        [Test]
        public async Task Logout_UserAuthenticated_ReturnsNoContent()
        {
            // Arrange
            string userId = "1";
            SetupUserContext(userId); // Simulate authenticated user
            _mockAuthService.Setup(s => s.LogoutAsync(userId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(true); // Simulate success

            // Act
            var result = await _authController.Logout(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }


        [Test]
        public async Task Logout_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            // Simulate user without NameIdentifier claim
            var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _authController.ControllerContext = new ControllerContext { HttpContext = httpContext };


            // Act
            var result = await _authController.Logout(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            _mockAuthService.Verify(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Logout_ServiceReturnsFailure_ReturnsBadRequest()
        {
            // Arrange
            string userId = "1";
            SetupUserContext(userId);
            _mockAuthService.Setup(s => s.LogoutAsync(userId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(false); // Simulate failure

            // Act
            var result = await _authController.Logout(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        #endregion

        #region GetMyInfo Tests

        [Test]
        public async Task GetMyInfo_UserAuthenticated_ReturnsOkWithUserInfo()
        {
            // Arrange
            string userId = "5";
            string email = "user@example.com";
            string fullName = "Test User Five";
            var roles = new List<string> { "User", "Reader" };
            SetupUserContext(userId, email, fullName, roles);

            // Act
            var result = _authController.GetMyInfo(); // This is synchronous

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult?.Value);
            // Use anonymous type for comparison as controller returns one
            dynamic userInfo = okResult.Value;
            Assert.AreEqual(userId, userInfo.GetType().GetProperty("Id").GetValue(userInfo, null));
            Assert.AreEqual(email, userInfo.GetType().GetProperty("Email").GetValue(userInfo, null));
            Assert.AreEqual(fullName, userInfo.GetType().GetProperty("FullName").GetValue(userInfo, null));
            CollectionAssert.AreEquivalent(roles, userInfo.GetType().GetProperty("Roles").GetValue(userInfo, null) as List<string>);
        }

        [Test]
        public async Task GetMyInfo_MissingClaims_ReturnsOkWithPartialInfo()
        {
            // Arrange
            string userId = "6";
            // Only ID claim is present
            SetupUserContext(userId);

            // Act
            var result = _authController.GetMyInfo();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult?.Value);
            dynamic userInfo = okResult.Value;
            Assert.AreEqual(userId, userInfo.GetType().GetProperty("Id").GetValue(userInfo, null));
            Assert.IsNull(userInfo.GetType().GetProperty("Email").GetValue(userInfo, null));
            Assert.IsNull(userInfo.GetType().GetProperty("FullName").GetValue(userInfo, null));
            Assert.IsEmpty(userInfo.GetType().GetProperty("Roles").GetValue(userInfo, null) as List<string>);
        }

        [Test]
        public async Task GetMyInfo_NoUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            // Simulate user without NameIdentifier claim
            var identity = new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Email, "only@email.com") }, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _authController.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = _authController.GetMyInfo();

            // Assert
            Assert.IsInstanceOf<UnauthorizedResult>(result);
        }

        #endregion

        #region GoogleSignIn Tests

        [Test]
        public async Task GoogleSignIn_ValidCredential_ReturnsOkWithTokens()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = "valid_google_id_token" };
            var tokens = new TokenResponseDto { AccessToken = "g_access", RefreshToken = "g_refresh" };
            // ** FIX: Use ValueTuple for service result **
            var serviceResult = (Success: true, Tokens: tokens, ErrorMessage: (string?)null);
            // ** FIX: Setup mock with matching tuple type **
            _mockAuthService.Setup(s => s.GoogleSignInAsync(request.Credential, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(tokens, okResult?.Value);
        }

        [Test]
        public async Task GoogleSignIn_MissingCredential_ReturnsBadRequest()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = "" }; // Empty credential

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            _mockAuthService.Verify(s => s.GoogleSignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GoogleSignIn_NullCredential_ReturnsBadRequest()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = null }; // Null credential

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            _mockAuthService.Verify(s => s.GoogleSignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GoogleSignIn_InvalidCredential_ReturnsUnauthorized()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = "invalid_google_id_token" };
            // ** FIX: Use ValueTuple for service result **
            var serviceResult = (Success: false, Tokens: (TokenResponseDto?)null, ErrorMessage: "Invalid Google token");
            // ** FIX: Setup mock with matching tuple type **
            _mockAuthService.Setup(s => s.GoogleSignInAsync(request.Credential, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult?.Value);
            dynamic responseValue = unauthorizedResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
        }

        [Test]
        public async Task GoogleSignIn_ServiceReturnsOtherError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = "valid_google_id_token" };
            // ** FIX: Use ValueTuple for service result **
            var serviceResult = (Success: false, Tokens: (TokenResponseDto?)null, ErrorMessage: "User creation failed");
            // ** FIX: Setup mock with matching tuple type **
            _mockAuthService.Setup(s => s.GoogleSignInAsync(request.Credential, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(serviceResult);

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            Assert.IsNotNull(objectResult?.Value);
            dynamic responseValue = objectResult.Value;
            Assert.AreEqual(serviceResult.ErrorMessage, responseValue.GetType().GetProperty("message").GetValue(responseValue, null));
        }

        [Test]
        public async Task GoogleSignIn_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new GoogleSignInRequestDto { Credential = "valid_google_id_token" };
            var exception = new Exception("Unexpected service error");
            _mockAuthService.Setup(s => s.GoogleSignInAsync(request.Credential, It.IsAny<CancellationToken>()))
                           .ThrowsAsync(exception);

            // Act
            var result = await _authController.GoogleSignIn(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);
            _mockLogger.Verify(
              x => x.Log(
                  LogLevel.Error,
                  It.IsAny<EventId>(),
                  It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unexpected error during Google Sign-In processing")),
                  exception,
                  It.IsAny<Func<It.IsAnyType, Exception, string>>()),
              Times.Once);
        }

        #endregion
    }
}