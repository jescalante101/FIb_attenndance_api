
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Manager
{
    [Table("AppUser")]
    public class AppUser
    {
        [Key]
        [Column("userId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [Column("userName")]
        [StringLength(50)]
        public string? UserName { get; set; }

        //[Required]
        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        [Column("password_hash")]
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        // Para los campos opcionales (que pueden ser NULL en la BD),
        // usamos el '?' para indicar que pueden ser nulos en C#.
        [Column("first_name")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Column("last_name")]
        [StringLength(50)]
        public string? LastName { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Column("updated_by")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<AppUserSite> AppUserSites { get; set; } = new List<AppUserSite>();

        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
