using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models.DTOs.BookRating
{
    public class CreateBookRatingDto
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int StarRating { get; set; }

        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }

        // UserId sẽ được lấy từ token của người dùng đang đăng nhập
        // RatingDate sẽ được gán ở server
    }
}
