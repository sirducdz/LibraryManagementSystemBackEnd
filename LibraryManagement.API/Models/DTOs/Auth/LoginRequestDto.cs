using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
