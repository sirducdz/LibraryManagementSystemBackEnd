using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.DTOs.User;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<(bool Success, UserProfileDto? UpdatedProfile, string? ErrorMessage)> UpdateProfileAsync(int userId, UpdateUserProfileDto profileDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin profile của người dùng theo ID.
        /// </summary>
        Task<UserProfileDto?> GetUserProfileByIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<PagedResult<UserDto>> GetAllUsersAsync(UserQueryParameters queryParams, CancellationToken cancellationToken = default);
        Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default); // Lấy chi tiết user (khác GetMyProfile)
        Task<(bool Success, UserDto? CreatedUser, string? ErrorMessage)> CreateUserAsync(CreateUserDto userDto, CancellationToken cancellationToken = default);
        Task<(bool Success, UserDto? UpdatedUser, string? ErrorMessage)> UpdateUserAsync(int userId, UpdateUserDto userDto, CancellationToken cancellationToken = default);
        Task<(bool Success, UserDto? UpdatedUser, string? ErrorMessage)> UpdateUserStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default); // Xem xét soft delete
    }
}
