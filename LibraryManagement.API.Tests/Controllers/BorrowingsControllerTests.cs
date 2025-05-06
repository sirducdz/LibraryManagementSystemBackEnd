using LibraryManagement.API.Controllers;
using LibraryManagement.API.Models.DTOs.Borrowing;
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
    public class BorrowingsControllerTests
    {
        private Mock<IBorrowingService> _mockBorrowingService;
        private Mock<ILogger<BorrowingsController>> _mockLogger;
        private BorrowingsController _borrowingsController;

        [SetUp]
        public void Setup()
        {
            _mockBorrowingService = new Mock<IBorrowingService>();
            _mockLogger = new Mock<ILogger<BorrowingsController>>();
            _borrowingsController = new BorrowingsController(_mockBorrowingService.Object, _mockLogger.Object);

            // Default HttpContext for non-authorized endpoints or to be overridden
            _borrowingsController.ControllerContext = new ControllerContext
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
            _borrowingsController.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region CreateBorrowingRequest Tests

        [Test]
        public async Task CreateBorrowingRequest_ValidData_ReturnsOkWithCreatedRequest()
        {
            // Arrange
            SetupUserContext("1");
            var requestDto = new CreateBorrowingRequestDto { BookIds = new List<int> { 101, 102 } };
            var createdRequestDto = new BorrowingRequestDto { Id = 1, RequestorId = 1, Status = "Pending" };
            _mockBorrowingService.Setup(s => s.CreateRequestAsync(requestDto, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((true, createdRequestDto, null));

            // Act
            var result = await _borrowingsController.CreateBorrowingRequest(requestDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result); // Controller returns Ok instead of CreatedAtAction
            var okResult = result as OkObjectResult;
            Assert.AreEqual(createdRequestDto, okResult?.Value);
        }

        [Test]
        public async Task CreateBorrowingRequest_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext("invalid-id"); // Non-integer user ID
            var requestDto = new CreateBorrowingRequestDto { BookIds = new List<int> { 1 } };

            // Act
            var result = await _borrowingsController.CreateBorrowingRequest(requestDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task CreateBorrowingRequest_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1");
            var requestDto = new CreateBorrowingRequestDto { BookIds = new List<int> { 999 } }; // Non-existent book
            _mockBorrowingService.Setup(s => s.CreateRequestAsync(requestDto, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "Book with ID 999 not found."));

            // Act
            var result = await _borrowingsController.CreateBorrowingRequest(requestDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task CreateBorrowingRequest_ServiceReturnsLimitReached_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext("1");
            var requestDto = new CreateBorrowingRequestDto { BookIds = new List<int> { 1, 2, 3, 4, 5, 6 } }; // Exceeds limit
            _mockBorrowingService.Setup(s => s.CreateRequestAsync(requestDto, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "Borrowing limit reached."));

            // Act
            var result = await _borrowingsController.CreateBorrowingRequest(requestDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
        #endregion

        #region GetMyRequests Tests
        [Test]
        public async Task GetMyRequests_UserAuthenticated_ReturnsOkWithPagedResult()
        {
            // Arrange
            SetupUserContext("1");
            var queryParams = new BorrowingRequestQueryParameters();
            var requests = new List<BorrowingRequestDto> { new BorrowingRequestDto { Id = 1, RequestorId = 1 } };
            var pagedResult = new PagedResult<BorrowingRequestDto>(requests, 1, 10, 1);
            _mockBorrowingService.Setup(s => s.GetMyRequestsAsync(1, queryParams, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(pagedResult);

            // Act
            var actionResult = await _borrowingsController.GetMyRequests(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.AreEqual(pagedResult, okResult?.Value);
        }

        [Test]
        public async Task GetMyRequests_InvalidUserIdClaim_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext("abc"); // Invalid user ID
            var queryParams = new BorrowingRequestQueryParameters();

            // Act
            var actionResult = await _borrowingsController.GetMyRequests(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(actionResult.Result);
        }
        #endregion

        #region GetAllRequests (Admin) Tests
        [Test]
        public async Task GetAllRequests_AdminUser_ReturnsOkWithPagedResult()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            var queryParams = new BorrowingRequestQueryParameters();
            var requests = new List<BorrowingRequestDto> { new BorrowingRequestDto { Id = 1 } };
            var pagedResult = new PagedResult<BorrowingRequestDto>(requests, 1, 10, 1);
            _mockBorrowingService.Setup(s => s.GetAllRequestsAsync(queryParams, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(pagedResult);

            // Act
            var actionResult = await _borrowingsController.GetAllRequests(queryParams, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
        }
        #endregion

        #region ApproveRequest (Admin) Tests
        [Test]
        public async Task ApproveRequest_AdminAndValidRequest_ReturnsOkWithUpdatedRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int requestId = 10;
            var updatedRequestDto = new BorrowingRequestDto { Id = requestId, Status = "Approved" };
            _mockBorrowingService.Setup(s => s.ApproveRequestAsync(requestId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((true, updatedRequestDto, null));

            // Act
            var result = await _borrowingsController.ApproveRequest(requestId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedRequestDto, okResult?.Value);
        }

        [Test]
        public async Task ApproveRequest_RequestNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int requestId = 99;
            _mockBorrowingService.Setup(s => s.ApproveRequestAsync(requestId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "Request not found."));

            // Act
            var result = await _borrowingsController.ApproveRequest(requestId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }
        #endregion

        #region RejectRequest (Admin) Tests
        [Test]
        public async Task RejectRequest_AdminAndValidRequest_ReturnsOkWithUpdatedRequest()
        {
            // Arrange
            SetupUserContext("1", new[] { "Admin" });
            int requestId = 10;
            var rejectDto = new RejectBorrowingRequestDto { Reason = "Book unavailable" };
            var updatedRequestDto = new BorrowingRequestDto { Id = requestId, Status = "Rejected", RejectionReason = "Book unavailable" };
            _mockBorrowingService.Setup(s => s.RejectRequestAsync(requestId, 1, rejectDto.Reason, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((true, updatedRequestDto, null));

            // Act
            var result = await _borrowingsController.RejectRequest(requestId, rejectDto, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(updatedRequestDto, okResult?.Value);
        }
        #endregion

        #region CancelMyRequest Tests
        [Test]
        public async Task CancelMyRequest_UserOwnsRequest_ReturnsOkWithUpdatedRequest()
        {
            // Arrange
            SetupUserContext("1");
            int requestId = 5;
            var updatedRequestDto = new BorrowingRequestDto { Id = requestId, RequestorId = 1, Status = "Cancelled" };
            _mockBorrowingService.Setup(s => s.CancelRequestAsync(requestId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((true, updatedRequestDto, null));

            // Act
            var result = await _borrowingsController.CancelMyRequest(requestId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task CancelMyRequest_RequestNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1");
            int requestId = 99;
            _mockBorrowingService.Setup(s => s.CancelRequestAsync(requestId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "Request not found."));
            // Act
            var result = await _borrowingsController.CancelMyRequest(requestId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task CancelMyRequest_UserNotAuthorized_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("2"); // Different user
            int requestIdOwnedByAnother = 5;
            _mockBorrowingService.Setup(s => s.CancelRequestAsync(requestIdOwnedByAnother, 2, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "User not authorized to cancel this request."));
            // Act
            var result = await _borrowingsController.CancelMyRequest(requestIdOwnedByAnother, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<ForbidResult>(result);
        }
        #endregion

        #region GetBorrowingRequestById Tests
        [Test]
        public async Task GetBorrowingRequestById_RequestExistsAndUserAuthorized_ReturnsOk()
        {
            // Arrange
            SetupUserContext("1"); // Assuming user "1" is the requestor or an admin
            int requestId = 1;
            var detailViewDto = new BorrowingRequestDetailViewDto
            {
                Id = requestId,
                RequestorId = 1,
                RequestorFullName = "Test User",
                DateRequested = DateTime.UtcNow.AddDays(-1),
                Status = "Pending",
                Details = new List<BookInfoForRequestDetailDto> {
                    new BookInfoForRequestDetailDto { BookId = 101, Title = "Book A" }
                }
            };
            _mockBorrowingService.Setup(s => s.GetRequestByIdAsync(requestId, 1, false, It.IsAny<CancellationToken>())) // Assuming isAdmin is false for this user
                                 .ReturnsAsync(detailViewDto);
            // Act
            var actionResult = await _borrowingsController.GetBorrowingRequestById(requestId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<OkObjectResult>(actionResult.Result);
            var okResult = actionResult.Result as OkObjectResult;
            var returnedDto = okResult?.Value as BorrowingRequestDetailViewDto;
            Assert.IsNotNull(returnedDto);
            Assert.AreEqual(detailViewDto.Id, returnedDto.Id);
            Assert.AreEqual(detailViewDto.RequestorId, returnedDto.RequestorId);
        }

        [Test]
        public async Task GetBorrowingRequestById_RequestNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1");
            int requestId = 99;
            _mockBorrowingService.Setup(s => s.GetRequestByIdAsync(requestId, 1, false, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((BorrowingRequestDetailViewDto?)null);
            // Act
            var actionResult = await _borrowingsController.GetBorrowingRequestById(requestId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<NotFoundResult>(actionResult.Result);
        }
        #endregion

        #region ExtendDueDate Tests
        [Test]
        public async Task ExtendDueDate_ValidRequest_ReturnsOkWithUpdatedRequest()
        {
            // Arrange
            SetupUserContext("1");
            int detailId = 101;
            var updatedRequestDto = new BorrowingRequestDto { Id = 1, /* other properties */ };
            _mockBorrowingService.Setup(s => s.ExtendDueDateAsync(detailId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((true, updatedRequestDto, null));
            // Act
            var result = await _borrowingsController.ExtendDueDate(detailId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task ExtendDueDate_DetailNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("1");
            int detailId = 999;
            _mockBorrowingService.Setup(s => s.ExtendDueDateAsync(detailId, 1, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "Detail not found."));
            // Act
            var result = await _borrowingsController.ExtendDueDate(detailId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task ExtendDueDate_NotAuthorized_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("2"); // Different user
            int detailId = 101;
            _mockBorrowingService.Setup(s => s.ExtendDueDateAsync(detailId, 2, It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((false, null, "User not authorized."));
            // Act
            var result = await _borrowingsController.ExtendDueDate(detailId, CancellationToken.None);
            // Assert
            Assert.IsInstanceOf<ForbidResult>(result);
        }
        #endregion
    }
}