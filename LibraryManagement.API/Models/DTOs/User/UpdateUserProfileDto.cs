using LibraryManagement.API.Models.Enums;

namespace LibraryManagement.API.Models.DTOs.User
{
    public class UpdateUserProfileDto
    {
        public string? FullName { get; set; }

        public Gender? Gender { get; set; }
    }
}
