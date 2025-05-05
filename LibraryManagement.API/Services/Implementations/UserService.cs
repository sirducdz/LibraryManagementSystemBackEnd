using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Models.DTOs.User;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Services.Interfaces;

namespace LibraryManagement.API.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        // private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger /*, IMapper mapper*/)
        {
            _userRepository = userRepository;
            _logger = logger;
            // _mapper = mapper;
        }

        public async Task<(bool Success, UserProfileDto? UpdatedProfile, string? ErrorMessage)> UpdateProfileAsync(int userId, UpdateUserProfileDto profileDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User {UserId} attempting to update their profile.", userId);

            try
            {
                var userToUpdate = await _userRepository.GetByIdAsync(userId, cancellationToken);

                if (userToUpdate == null || !userToUpdate.IsActive) // Chỉ cho user active cập nhật
                {
                    _logger.LogWarning("Update profile failed: User {UserId} not found or inactive.", userId);
                    return (false, null, "User not found or inactive.");
                }

                // Cập nhật các trường được phép từ DTO
                // Dùng ! vì validation nên đảm bảo FullName không null
                userToUpdate.FullName = profileDto.FullName!.Trim();
                if (profileDto.Gender.HasValue) // Chỉ cập nhật Gender nếu được cung cấp
                {
                    userToUpdate.Gender = profileDto.Gender.Value;
                }
                userToUpdate.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian

                // Lưu thay đổi (Repo auto-save)
                int updatedCount = await _userRepository.UpdateAsync(userToUpdate, cancellationToken);

                if (updatedCount > 0)
                {
                    _logger.LogInformation("User {UserId} profile updated successfully.", userId);
                    // Map entity đã cập nhật sang DTO để trả về
                    var updatedProfileDto = MapToUserProfileDto(userToUpdate);
                    return (true, updatedProfileDto, null);
                }
                else
                {
                    _logger.LogWarning("Update profile failed for User {UserId}: UpdateAsync returned 0.", userId);
                    return (false, null, "Failed to update profile. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for User {UserId}", userId);
                return (false, null, "An unexpected error occurred while updating profile.");
            }
        }

        public async Task<UserProfileDto?> GetUserProfileByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching profile for User ID: {UserId}", userId);
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null || !user.IsActive) return null; // Chỉ trả về user active
                return MapToUserProfileDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for User ID: {UserId}", userId);
                return null;
            }
        }

        // Hàm helper để map User sang UserProfileDto
        private UserProfileDto MapToUserProfileDto(User user)
        {
            // Nếu dùng AutoMapper thì không cần hàm này
            if (user == null) return null!;
            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
