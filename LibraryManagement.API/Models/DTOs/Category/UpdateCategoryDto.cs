using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Category
{
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
