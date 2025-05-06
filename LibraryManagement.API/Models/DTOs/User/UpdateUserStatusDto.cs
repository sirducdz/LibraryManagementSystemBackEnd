using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class UpdateUserStatusDto
    {
        [Required] // Bắt buộc phải có giá trị true hoặc false
        public bool IsActive { get; set; }
    }
}
