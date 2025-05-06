using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Dashboard;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LibraryManagement.API.Tests.Controllers
{
    [TestFixture]
    public class DashboardControllerTests
    {
        private Mock<IDashboardService> _mockDashboardService;
        private Mock<ILogger<DashboardController>> _mockLogger;
        private DashboardController _dashboardController;

        [SetUp]
        public void Setup()
        {
            _mockDashboardService = new Mock<IDashboardService>();
            _mockLogger = new Mock<ILogger<DashboardController>>();
            _dashboardController = new DashboardController(_mockDashboardService.Object, _mockLogger.Object);

            // Simulate an authenticated Admin user for all tests in this controller
            SetupAdminUserContext("1");
        }

        // Helper to simulate an authenticated Admin user
        private void SetupAdminUserContext(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Admin") // Crucial for [Authorize(Roles = "Admin")]
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _dashboardController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetDashboardData Tests

        [Test]
        public async Task GetDashboardData_ServiceReturnsNull_ReturnsOkWithNullData() // Or handle as error
        {
            // Arrange
            _mockDashboardService.Setup(s => s.GetDashboardDataAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((DashboardDto?)null);

            // Act
            var actionResult = await _dashboardController.GetDashboardData(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNull(okResult?.Value); // Service returned null, controller passes it through as Ok(null)
        }


        [Test]
        public async Task GetDashboardData_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var exceptionMessage = "Simulated service error";
            var exception = new Exception(exceptionMessage);
            _mockDashboardService.Setup(s => s.GetDashboardDataAsync(It.IsAny<CancellationToken>()))
                                 .ThrowsAsync(exception);

            // Act
            var actionResult = await _dashboardController.GetDashboardData(CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(actionResult.Result);
            var objectResult = actionResult.Result as ObjectResult;
            Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult?.StatusCode);

            Assert.IsNotNull(objectResult?.Value);
            var responseValue = objectResult.Value;
            // Check the message structure returned by the controller
            var messageProperty = responseValue.GetType().GetProperty("message");
            Assert.IsNotNull(messageProperty, "Response object should have a 'message' property.");
            Assert.AreEqual("An error occurred while retrieving dashboard data.", messageProperty.GetValue(responseValue, null));


            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving dashboard data.")),
                   exception, // Verify the exact exception is logged
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
        }

        // Note: Testing [Authorize(Roles = "Admin")] for non-admin or unauthenticated users
        // is typically better handled by integration tests that involve the full ASP.NET Core pipeline.
        // In unit tests, we assume authorization has passed if we set up the User context correctly.
        // However, we can add a test to ensure the attribute is present, though this is more reflective testing.

        [Test]
        public void GetDashboardData_Action_HasAdminAuthorizeAttribute()
        {
            // Arrange
            var methodInfo = typeof(DashboardController).GetMethod(nameof(DashboardController.GetDashboardData));
            var controllerAttributes = typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true);
            var methodAttributes = methodInfo?.GetCustomAttributes(typeof(AuthorizeAttribute), true);

            // Assert
            // Check if controller itself has [Authorize(Roles="Admin")]
            var controllerAuthorizeAttribute = controllerAttributes.FirstOrDefault() as AuthorizeAttribute;
            Assert.IsNotNull(controllerAuthorizeAttribute, "Controller should have Authorize attribute.");
            Assert.AreEqual("Admin", controllerAuthorizeAttribute.Roles, "Controller Authorize attribute should specify Admin role.");

            // The GetDashboardData action itself does not have an additional Authorize attribute,
            // it inherits from the controller. So methodAttributes should be empty or not have a specific role.
            // If the action had its own [Authorize] attribute, you would check it here.
            Assert.IsTrue(methodAttributes == null || !methodAttributes.Any(), "GetDashboardData action should not have its own Authorize attribute if controller is already authorized for Admin.");
        }


        #endregion
    }
}