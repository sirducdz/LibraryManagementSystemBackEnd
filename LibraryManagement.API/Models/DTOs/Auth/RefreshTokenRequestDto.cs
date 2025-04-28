using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
