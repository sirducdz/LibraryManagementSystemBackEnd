namespace LibraryManagement.API.Models.DTOs.Auth
{
    public class TokenRequestDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
