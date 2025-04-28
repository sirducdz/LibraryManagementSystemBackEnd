using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("UserActivityLogs")]
    public class UserActivityLog : IEntity<long>
    {
        [Key]
        public long Id { get; set; }

        public int? UserID { get; set; } // Foreign Key, nullable

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TargetEntityType { get; set; }

        [MaxLength(100)]
        public string? TargetEntityID { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Details { get; set; } // Store details as JSON or XML

        [MaxLength(50)]
        public string? SourceIPAddress { get; set; }

        // ----- Navigation Property -----
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}
