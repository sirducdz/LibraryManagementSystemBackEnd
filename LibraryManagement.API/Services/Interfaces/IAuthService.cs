using LibraryManagement.API.Models.DTOs.Auth;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default);
        Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> RefreshTokenAsync(TokenRequestDto providedToken, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
        Task<(bool Success, string? UserId, string? ErrorMessage)> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken = default);
        Task<(bool Success, TokenResponseDto? Tokens, string? ErrorMessage)> GoogleSignInAsync(string googleIdToken, CancellationToken cancellationToken = default);
    }
}
