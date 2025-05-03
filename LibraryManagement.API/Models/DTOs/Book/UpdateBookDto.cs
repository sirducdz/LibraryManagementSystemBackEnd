using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.Book
{
    public class UpdateBookDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Author { get; set; }

        [MaxLength(20)]
        public string? ISBN { get; set; }

        [MaxLength(100)]
        public string? Publisher { get; set; }

        public int? PublicationYear { get; set; }

        public string? Description { get; set; }

        public string? CoverImageUrl { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int TotalQuantity { get; set; }
    }
}
