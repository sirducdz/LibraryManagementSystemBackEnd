using System.Security.Claims;

namespace LibraryManagement.API.Services.Interfaces
{
    public interface ITokenService
    {
        // Tạo Access Token dựa trên thông tin User và Roles
        (string AccessToken, string Jti) GenerateAccessToken(IEnumerable<Claim> claims);

        // Tạo Refresh Token ngẫu nhiên
        string GenerateRefreshToken();

        // Lấy Principal từ Access Token đã hết hạn (để đọc thông tin user khi refresh)
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
