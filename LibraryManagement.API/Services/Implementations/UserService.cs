using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Helpers;
using LibraryManagement.API.Models.DTOs.Common;
using LibraryManagement.API.Models.DTOs.QueryParameters;
using LibraryManagement.API.Models.DTOs.User;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LibraryManagement.API.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IUserRefreshTokenRepository _refreshTokenRepository; // << Inject để thu hồi token
        private readonly PasswordHasher _passwordHasher;
        // private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger /*, IMapper mapper*/, IUserRefreshTokenRepository refreshTokenRepository, PasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _logger = logger;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
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
        // --- CÁC HÀM QUẢN LÝ USER CHO ADMIN ---

        public async Task<PagedResult<UserDto>> GetAllUsersAsync(UserQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all users with parameters: {@QueryParams}", queryParams);
            try
            {
                var query = _userRepository.GetAllQueryable() // << Dùng hàm lấy cả user bị xóa mềm nếu User có IsDeleted
                                          .Include(u => u.Role)
                                          .AsQueryable(); // Include Role để lấy RoleName

                // --- Áp dụng Filter ---
                if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
                {
                    var term = queryParams.SearchTerm.Trim().ToLower();
                    query = query.Where(u => u.UserName.ToLower().Contains(term) ||
                                             u.FullName.ToLower().Contains(term) ||
                                             u.Email.ToLower().Contains(term));
                }
                if (queryParams.RoleId.HasValue)
                {
                    query = query.Where(u => u.RoleID == queryParams.RoleId.Value);
                }
                if (queryParams.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == queryParams.IsActive.Value);
                }

                // --- Áp dụng Sorting ---
                Expression<Func<User, object>> keySelector = queryParams.SortBy?.ToLowerInvariant() switch
                {
                    "username" => u => u.UserName,
                    "fullname" => u => u.FullName,
                    "email" => u => u.Email,
                    "role" => u => u.Role!.RoleName, // Sắp xếp theo tên Role (cần Role not null)
                    "isactive" => u => u.IsActive,
                    "createdat" => u => u.CreatedAt,
                    _ => u => u.UserName // Mặc định
                };
                if (string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                    query = query.OrderByDescending(keySelector);
                else
                    query = query.OrderBy(keySelector);


                // --- Phân trang ---
                var totalItems = await query.CountAsync(cancellationToken);
                var users = await query
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToListAsync(cancellationToken);

                // Map sang DTO
                var userDtos = users.Select(u => MapToUserDto(u)).ToList(); // Dùng hàm helper
                return new PagedResult<UserDto>(userDtos, queryParams.Page, queryParams.PageSize, totalItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users with query: {@QueryParams}", queryParams);
                // Trả về lỗi hoặc kết quả rỗng
                return new PagedResult<UserDto>(new List<UserDto>(), queryParams.Page, queryParams.PageSize, 0);
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching user by ID: {UserId} for admin view", userId);
            var user = await _userRepository.GetAllQueryable() // Lấy cả user bị xóa mềm
                                          .Include(u => u.Role)
                                          .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return null;
            return MapToUserDto(user);
        }
        private UserDto MapToUserDto(User user)
        {
            if (user == null) return null!;
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                RoleID = user.RoleID,
                RoleName = user.Role?.RoleName, // Cần Include Role
                IsActive = user.IsActive,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
        public async Task<(bool Success, UserDto? CreatedUser, string? ErrorMessage)> CreateUserAsync(CreateUserDto userDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin attempting to create user: {UserName}, Email: {Email}", userDto.UserName, userDto.Email);
            try
            {
                // Kiểm tra trùng UserName và Email
                if (await _userRepository.ExistsAsync(u => u.UserName == userDto.UserName, cancellationToken))
                    return (false, null, "Username already exists.");
                if (await _userRepository.ExistsAsync(u => u.Email == userDto.Email, cancellationToken))
                    return (false, null, "Email already exists.");

                // Kiểm tra RoleID hợp lệ (nếu cần)
                // bool roleExists = await _roleRepository.ExistsAsync(r => r.Id == userDto.RoleID);
                // if (!roleExists) return (false, null, "Invalid RoleID.");

                var hashedPassword = _passwordHasher.HashPassword(userDto.Password!);

                var newUser = new User
                {
                    UserName = userDto.UserName!.Trim(),
                    Email = userDto.Email!.Trim(),
                    FullName = userDto.FullName!.Trim(),
                    PasswordHash = hashedPassword,
                    RoleID = userDto.RoleID,
                    IsActive = userDto.IsActive ?? true, // Lấy giá trị hoặc mặc định true
                    Gender = userDto.Gender ?? Gender.Unknown,
                    CreatedAt = DateTime.UtcNow,
                    //IsDeleted = false // Giả sử có IsDeleted và mặc định là false
                };

                int addedCount = await _userRepository.AddAsync(newUser, cancellationToken);
                if (addedCount > 0)
                {
                    _logger.LogInformation("Admin successfully created user ID {UserId}", newUser.Id);
                    // Lấy lại thông tin user vừa tạo kèm Role để trả về DTO
                    var createdUser = await _userRepository.GetAllQueryable().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == newUser.Id, cancellationToken);
                    return (true, MapToUserDto(createdUser!), null); // Map sang DTO
                }
                else
                {
                    _logger.LogError("Failed to create user {UserName}: AddAsync returned 0.", userDto.UserName);
                    return (false, null, "Failed to save the new user.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {UserName}", userDto.UserName);
                return (false, null, "An unexpected error occurred.");
            }
        }

        public async Task<(bool Success, UserDto? UpdatedUser, string? ErrorMessage)> UpdateUserAsync(int userId, UpdateUserDto userDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin attempting to update user ID: {UserId}", userId);
            try
            {
                var userToUpdate = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (userToUpdate == null) return (false, null, "User not found.");

                // Kiểm tra RoleID hợp lệ nếu được cập nhật
                // if (userToUpdate.RoleID != userDto.RoleID) {
                //     bool roleExists = await _roleRepository.ExistsAsync(r => r.Id == userDto.RoleID);
                //     if (!roleExists) return (false, null, "Invalid RoleID.");
                // }

                // Cập nhật các trường cho phép
                userToUpdate.FullName = userDto.FullName!.Trim();
                userToUpdate.RoleID = userDto.RoleID;
                if (userDto.Gender.HasValue) userToUpdate.Gender = userDto.Gender.Value;
                // if(userDto.IsActive.HasValue) userToUpdate.IsActive = userDto.IsActive.Value; // Cập nhật IsActive nếu DTO có
                userToUpdate.UpdatedAt = DateTime.UtcNow;

                int updatedCount = await _userRepository.UpdateAsync(userToUpdate, cancellationToken);
                if (updatedCount > 0)
                {
                    _logger.LogInformation("Admin successfully updated user ID {UserId}", userId);
                    var updatedUser = await _userRepository.GetAllQueryable().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                    return (true, MapToUserDto(updatedUser!), null);
                }
                else
                {
                    return (false, null, "Failed to update user.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID: {UserId}", userId);
                return (false, null, "An unexpected error occurred.");
            }
        }

        public async Task<(bool Success, UserDto? UpdatedUser, string? ErrorMessage)> UpdateUserStatusAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin attempting to set IsActive={IsActive} for user ID: {UserId}", isActive, userId);
            try
            {
                var userToUpdate = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (userToUpdate == null) return (false, null, "User not found.");

                if (userToUpdate.IsActive == isActive) // Không có gì thay đổi
                    return (true, MapToUserDto(userToUpdate), null);

                userToUpdate.IsActive = isActive;
                userToUpdate.UpdatedAt = DateTime.UtcNow;

                // >>> Thu hồi Refresh Tokens nếu Deactivate <<<
                if (!isActive)
                {
                    _logger.LogInformation("User {UserId} deactivated, revoking refresh tokens.", userId);
                    // Không cần await nếu không muốn chờ kết quả ở đây
                    _ = await _refreshTokenRepository.RevokeTokensByUserIdAsync(userId, cancellationToken);
                }

                int updatedCount = await _userRepository.UpdateAsync(userToUpdate, cancellationToken);
                if (updatedCount > 0)
                {
                    _logger.LogInformation("Admin successfully updated status for user ID {UserId}", userId);
                    var updatedUser = await _userRepository.GetAllQueryable().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                    return (true, MapToUserDto(updatedUser!), null);
                }
                else
                {
                    return (false, null, "Failed to update user status.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user ID: {UserId}", userId);
                return (false, null, "An unexpected error occurred.");
            }
        }


        public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Admin attempting to delete user ID: {UserId}", userId);
            try
            {
                var userToDelete = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (userToDelete == null) return (false, "User not found.");

                // Thực hiện Hard Delete (vì User entity không có IsDeleted)
                //int deletedCount = await _userRepository.RemoveAsync(userToDelete, cancellationToken);

                // HOẶC: Thực hiện Soft Delete (nếu User có IsDeleted)
                userToDelete.IsDeleted = true;
                userToDelete.IsActive = false; // Nên deactivate luôn
                userToDelete.UpdatedAt = DateTime.UtcNow;
                int deletedCount = await _userRepository.UpdateAsync(userToDelete, cancellationToken);
                if (deletedCount > 0)
                    await _refreshTokenRepository.RevokeTokensByUserIdAsync(userId, cancellationToken); // Thu hồi token nếu soft delete

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Admin successfully deleted user ID {UserId}", userId);
                    return (true, null);
                }
                else
                {
                    return (false, "Failed to delete user.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID: {UserId}", userId);
                return (false, "An unexpected error occurred.");
            }
        }
    }
}
