using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("UserRefreshTokens")]
    public class UserRefreshToken : IEntity<Guid> // Kế thừa IEntity<Guid>
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign Key đến bảng Users (vẫn là int để khớp với User.Id)

        [Required]
        [MaxLength(500)] // Độ dài tùy thuộc vào cách tạo token
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        // Sử dụng UtcNow để tránh vấn đề về múi giờ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; } // Thời điểm token bị thu hồi (UTC)
        [Required] // Quan trọng: Cần có giá trị mặc định là false
        public bool IsRevoked { get; set; } = false;
        //public string? ReplacedByToken { get; set; } // Token mới thay thế token này

        public string? ReasonRevoked { get; set; } // Lý do thu hồi

        // Thuộc tính tính toán để kiểm tra token còn hoạt động không
        [NotMapped] // Không ánh xạ vào cơ sở dữ liệu
        public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

        // Thông tin tùy chọn để định danh thiết bị/phiên
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // ----- Navigation Property -----
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
