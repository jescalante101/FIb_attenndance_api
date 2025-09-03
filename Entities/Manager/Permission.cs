using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Manager
{
    /// <summary>
    /// Representa un permiso dentro del sistema, mapeado desde la tabla [Permissions].
    /// </summary>
    [Table("Permissions")]
    public class Permission
    {
        [Key]
        [Column("permission_id")]
        public int PermissionId { get; set; }

        [Required]
        [Column("permission_key")]
        [StringLength(100)]
        public string PermissionKey { get; set; }

        [Required]
        [Column("permission_name")]
        [StringLength(50)]
        public string PermissionName { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

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

        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
