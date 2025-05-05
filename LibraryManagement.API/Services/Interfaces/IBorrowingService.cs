using LibraryManagement.API.Models.DTOs.Borrowing;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IBorrowingService
    {
        Task<(bool Success, BorrowingRequestDto? CreatedRequest, string? ErrorMessage)> CreateRequestAsync(CreateBorrowingRequestDto requestDto, int userId, CancellationToken cancellationToken = default);
        Task<PagedResult<BorrowingRequestDto>> GetMyRequestsAsync(int userId, BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken = default);

        Task<PagedResult<BorrowingRequestDto>> GetAllRequestsAsync(BorrowingRequestQueryParameters queryParams, CancellationToken cancellationToken = default);

        // Trả về DTO đã cập nhật để hiển thị trạng thái mới
        Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> ApproveRequestAsync(int requestId, int approverUserId, CancellationToken cancellationToken = default);

        Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> RejectRequestAsync(int requestId, int approverUserId, string? reason, CancellationToken cancellationToken = default);
        Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> CancelRequestAsync(int requestId, int userId, CancellationToken cancellationToken = default);
        Task<BorrowingRequestDetailViewDto?> GetRequestByIdAsync(int requestId, int userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<(bool Success, BorrowingRequestDto? UpdatedRequest, string? ErrorMessage)> ExtendDueDateAsync(int detailId, int userId, CancellationToken cancellationToken = default);
    }
}
