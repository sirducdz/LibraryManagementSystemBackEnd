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
    }
}
