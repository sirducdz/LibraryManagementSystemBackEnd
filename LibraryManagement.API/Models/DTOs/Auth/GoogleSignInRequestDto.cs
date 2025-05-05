using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Auth
{
    public class GoogleSignInRequestDto
    {
        [Required]
        public string? Credential { get; set; }
    }
}
