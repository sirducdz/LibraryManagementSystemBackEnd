using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models.Entities
{
    [Table("Roles")]
    public class Role : IEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
