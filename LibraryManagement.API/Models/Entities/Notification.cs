using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("Notifications")]
    public class Notification : IEntity<long>
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public int UserID { get; set; } // Foreign Key
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityID { get; set; }
        [Required]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation Property
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
