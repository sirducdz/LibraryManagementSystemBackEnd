using Google.Apis.Auth;
using LibraryManagement.API.Configuration;
using LibraryManagement.API.Data.Repositories.Interfaces;
using LibraryManagement.API.Helpers;
using LibraryManagement.API.Models.DTOs.Auth;
using LibraryManagement.API.Models.Entities;
using LibraryManagement.API.Models.Enums;
using LibraryManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LibraryManagement.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly PasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;
        private readonly GoogleAuthSettings _googleAuthSettings;

        public AuthService(
            IUserRepository userRepository,
            IUserRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            JwtSettings jwtSettings,
            PasswordHasher passwordHasher,
            ILogger<AuthService> logger, IOptions<GoogleAuthSettings> googleAuthSettingsOptions)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _jwtSettings = jwtSettings;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _googleAuthSettings = googleAuthSettingsOptions.Value;
        }

        public async Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.FindByUserNameAsync(loginRequest.UserName, cancellationToken);

            if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt failed for userName {UserName}: Invalid credentials or inactive user.", loginRequest.UserName);
                return (false, null, "Invalid credentials.");
            }

            // --- Tạo Tokens ---
            var authClaims = CreateClaims(user); // Hàm helper như trước
            var receivedToken = _tokenService.GenerateAccessToken(authClaims);
            var refreshTokenString = _tokenService.GenerateRefreshToken();

            // --- Tạo và Lưu Refresh Token Entity Mới ---
            var refreshTokenEntity = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                JwtId = receivedToken.Jti,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false // Mặc định là chưa bị thu hồi
            };

            try
            {
                // Thêm token mới vào bảng UserRefreshTokens (Repo tự động save)
                int addedCount = await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
                if (addedCount == 0)
                {
                    _logger.LogError("Failed to save refresh token for user {UserId} during login.", user.Id);
                    return (false, null, "Login failed due to a server error.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token for user {UserId} during login.", user.Id);
                return (false, null, "Login failed due to a server error.");
            }

            _logger.LogInformation("User {UserId} logged in successfully. Refresh token generated.", user.Id);
            return (true, new TokenResponseDto
            {
                AccessToken = receivedToken.AccessToken,
                RefreshToken = refreshTokenString, // Trả về chuỗi token
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, null);
        }

        public async Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> RefreshTokenAsync(TokenRequestDto providedToken, CancellationToken cancellationToken = default)
        {

            var TokenClaims = _tokenService.GetPrincipalFromExpiredToken(providedToken.AccessToken);
            if (TokenClaims == null)
            {
                return (false, null, "Invalid token.");
            }
            var utcExpiredDate = long.Parse(TokenClaims?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value);
            var expiredDate = DateTimeHelper.ConvertFromUnixSeconds(utcExpiredDate);
            if (expiredDate > DateTime.UtcNow)
            {
                return (false, null, "Access token has not expired.");
            }
            var storedToken = await _refreshTokenRepository.FindByTokenAsync(providedToken.RefreshToken, cancellationToken);
            #region MyRegion
            // Kiểm tra token có tồn tại, hợp lệ, chưa bị thu hồi và user có active không
            //if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow || storedToken.User == null || !storedToken.User.IsActive)
            //{
            //    // Nếu token không hợp lệ hoặc đã hết hạn/revoked -> Thu hồi tất cả token của user đó (đề phòng kẻ gian dùng token cũ)
            //    if (storedToken != null && storedToken.User != null)
            //    {
            //        _logger.LogWarning("Potential refresh token reuse detected for user {UserId}. Revoking all tokens.", storedToken.UserId);
            //        // Không cần await vì không cần chờ kết quả ở đây
            //        _ = await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);
            //    }
            //    else
            //    {
            //        _logger.LogWarning("Refresh token attempt failed: Invalid, expired, or revoked token provided.");
            //    }
            //    return (false, null, "Invalid or expired refresh token.");
            //} 
            #endregion
            if (storedToken == null || storedToken.User == null)
            {
                return (false, null, "Invalid refresh token.");
            }

            if (storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                _ = await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);
                return (false, null, "IsRevoked or expired refresh token.");
            }

            if (!storedToken.User.IsActive) // Kiểm tra xem User có đang active không
            {
                _ = await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);
                return (false, null, "User account is inactive."); // Thông báo lỗi rõ ràng hơn
            }
            var jti = TokenClaims?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (storedToken.JwtId != jti)
            {
                return (false, null, "Token is not match."); // Thông báo lỗi rõ ràng hơn
            }

            // --- Token hợp lệ -> Thực hiện xoay vòng token ---
            var user = storedToken.User; // Lấy user từ token đã include

            // Tạo claims và access token mới
            var authClaims = CreateClaims(user);
            var newReceivedToken = _tokenService.GenerateAccessToken(authClaims);

            // Tạo refresh token MỚI
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            var newRefreshTokenEntity = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                JwtId = newReceivedToken.Jti,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            try
            {
                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

                await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken); // Lưu token mới
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token rotation for user {UserId}.", user.Id);
                return (false, null, "Token refresh failed due to a server error.");
            }

            _logger.LogInformation("Token refreshed successfully for user {UserId}. Old token revoked, new token generated.", user.Id);
            return (true, new TokenResponseDto
            {
                AccessToken = newReceivedToken.AccessToken,
                RefreshToken = newRefreshTokenString, // Trả về token mới
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            }, null);
        }


        public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(userId, out int id))
            {
                _logger.LogWarning("Logout attempt failed: Invalid user ID format '{UserId}'.", userId);
                return false;
            }

            // Tìm và thu hồi TẤT CẢ refresh token đang hoạt động của user này
            _logger.LogInformation("Attempting to revoke all active refresh tokens for user {UserId}.", id);
            int revokedCount = await RevokeAllUserTokensAsync(id, cancellationToken);
            _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId} during logout.", revokedCount, id);

            return true;
        }


        // Hàm helper để thu hồi token
        private async Task<int> RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken)
        {
            var userTokens = await _refreshTokenRepository.FindByUserIdAsync(userId, cancellationToken);
            var activeTokens = userTokens.Where(t => !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow).ToList();

            if (!activeTokens.Any())
            {
                return 0; // Không có token nào đang hoạt động để thu hồi
            }

            int successfulRevocations = 0;
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                try
                {
                    int updateCount = await _refreshTokenRepository.UpdateAsync(token, cancellationToken); // Lưu thay đổi
                    if (updateCount > 0) successfulRevocations++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking refresh token ID {TokenId} for user {UserId}.", token.Id, userId);
                    // Tiếp tục thu hồi các token khác dù có lỗi
                }
            }
            return successfulRevocations;
        }

        public async Task<(bool Success, string? UserId, string? ErrorMessage)> RegisterAsync(RegisterRequestDto registerDto, CancellationToken cancellationToken = default)
        {
            try
            {
                bool emailExists = await _userRepository.ExistsAsync(u => u.Email == registerDto.Email, cancellationToken);
                if (emailExists)
                {
                    _logger.LogWarning("Registration attempt failed: Email {Email} already exists.", registerDto.Email);
                    return (false, null, "Email already exists.");
                }

                // Kiểm tra UserName
                bool userNameExists = await _userRepository.ExistsAsync(u => u.UserName == registerDto.UserName, cancellationToken);
                if (userNameExists)
                {
                    _logger.LogWarning("Registration attempt failed: UserName {UserName} already exists.", registerDto.UserName);
                    return (false, null, "Username already exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email/username existence for {Email} / {UserName}.", registerDto.Email, registerDto.UserName);
                return (false, null, "An error occurred while checking availability.");
            }
            var hashedPassword = _passwordHasher.HashPassword(registerDto.Password);

            var user = new User
            {
                UserName = registerDto.UserName!.Trim(),
                Email = registerDto.Email!.Trim(),
                PasswordHash = hashedPassword,
                FullName = registerDto.FullName!.Trim(),
                RoleID = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, // Sử dụng giờ UTC
                Gender = registerDto.Gender ?? Gender.Unknown, // Gán Gender, nếu null thì mặc định là Unknown
                UpdatedAt = null // Chưa cập nhật
            };

            try
            {
                // Gọi AddAsync của repository (giả định auto-save)
                int addedCount = await _userRepository.AddAsync(user, cancellationToken);

                if (addedCount > 0)
                {
                    // Thành công, user.Id đã có giá trị từ DB
                    _logger.LogInformation("User registered successfully with ID {UserId} and Email {Email}.", user.Id, user.Email);
                    return (true, user.Id.ToString(), null); // Trả về ID dạng string
                }
                else
                {
                    // Trường hợp hiếm gặp: không lỗi nhưng không có dòng nào được thêm
                    _logger.LogError("Registration failed for email {Email}: AddAsync returned 0 affected rows.", registerDto.Email);
                    return (false, null, "Registration failed due to a server error (code 1).");
                }
            }
            catch (Exception ex) // Bắt các lỗi không mong muốn khác
            {
                _logger.LogError(ex, "Unexpected error during registration for email {Email}.", registerDto.Email);
                return (false, null, "Registration failed due to an unexpected server error (code 3).");
            }
        }

        private List<Claim> CreateClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email), // Chuẩn JWT
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID duy nhất cho token
                new Claim(ClaimTypes.Name, user.FullName), // Thêm tên nếu cần
                new Claim(ClaimTypes.Role, user.Role.RoleName) // Thêm tên nếu cần
            };

            return claims;
        }

        public async Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> GoogleSignInAsync(string googleIdToken, CancellationToken cancellationToken = default)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                // 1. Xác thực Google ID Token
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuthSettings.ClientId } // Kiểm tra Audience phải là ClientID của bạn
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, validationSettings);
                _logger.LogInformation("Google ID Token validated for email: {Email}", payload.Email);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID Token received.");
                return (false, null, "Invalid Google token."); // Lỗi cụ thể cho client biết
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google ID Token.");
                return (false, null, "Error validating Google token."); // Lỗi chung chung hơn
            }

            // --- Token Google hợp lệ ---
            try
            {
                // 2. Tìm User trong DB bằng Email từ payload
                User? user = await _userRepository.FindByEmailAsync(payload.Email, cancellationToken);

                // 3. Xử lý User: Tạo mới nếu chưa có
                if (user == null)
                {
                    _logger.LogInformation("User with email {Email} not found. Registering new user via Google.", payload.Email);
                    // Tạo user mới
                    user = new User
                    {
                        UserName = payload.Email, // Dùng Email làm UserName
                        Email = payload.Email,
                        FullName = payload.Name ?? payload.Email, // Lấy tên từ Google, nếu không có dùng tạm Email
                        PasswordHash = "", // Không có mật khẩu cục bộ - Cần xử lý ở luồng login thường
                        RoleID = 2, // Vai trò mặc định (ví dụ: NormalUser = 2)
                        IsActive = true, // Kích hoạt ngay
                        CreatedAt = DateTime.UtcNow,
                        Gender = Gender.Unknown // Hoặc thử lấy từ payload nếu có
                                                // EmailVerified = payload.EmailVerified // Lưu trạng thái xác thực nếu cần
                    };

                    // Lưu user mới vào DB (Repo auto-save)
                    int addedCount = await _userRepository.AddAsync(user, cancellationToken);
                    if (addedCount <= 0)
                    {
                        _logger.LogError("Failed to register user via Google: AddAsync returned 0 for email {Email}.", payload.Email);
                        return (false, null, "Failed to create local user account.");
                    }
                    _logger.LogInformation("New user {UserId} registered via Google for email {Email}.", user.Id, payload.Email);
                }
                else if (!user.IsActive)
                {
                    // User tồn tại nhưng bị khóa
                    _logger.LogWarning("Google Sign-In attempt failed for email {Email}: User account is inactive.", payload.Email);
                    return (false, null, "Your account is inactive.");
                }
                else
                {
                    _logger.LogInformation("User {UserId} with email {Email} found locally. Proceeding with login.", user.Id, payload.Email);
                }

                // --- 4. Tạo Token của Hệ thống Bạn ---
                var authClaims = CreateClaims(user); // Tạo claims cho user này
                var (accessToken, generatedJti) = _tokenService.GenerateAccessToken(authClaims); // Lấy cả JTI trả về
                var refreshTokenString = _tokenService.GenerateRefreshToken();

                // Tạo và lưu Refresh Token mới vào DB
                var refreshTokenEntity = new UserRefreshToken
                {
                    UserId = user.Id,
                    Token = refreshTokenString,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                    JwtId = generatedJti, // Lưu JTI của Access Token tương ứng
                    IsRevoked = false
                };

                // Có thể thu hồi các token cũ của user ở đây nếu muốn
                await RevokeAllUserTokensAsync(user.Id, cancellationToken);

                await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken); // Repo auto-save

                _logger.LogInformation("Generated local tokens for User {UserId} after Google Sign-In.", user.Id);

                // 5. Trả về bộ token của hệ thống bạn
                return (true, new TokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenString,
                    AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                }, null);

            }
            catch (DbUpdateException dbEx) // Bắt lỗi DB cụ thể khi tạo user/token
            {
                _logger.LogError(dbEx, "Database error during Google Sign-In processing for email {Email}.", payload.Email);
                return (false, null, "A database error occurred during sign-in.");
            }
            catch (Exception ex) // Bắt lỗi chung khác
            {
                _logger.LogError(ex, "Unexpected error during Google Sign-In processing for email {Email}.", payload.Email);
                return (false, null, "An unexpected error occurred during sign-in.");
            }
        }
    }
}
