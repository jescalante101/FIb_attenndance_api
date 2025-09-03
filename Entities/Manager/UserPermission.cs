using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Manager
{

    /// <summary>
    /// Entidad que mapea la relación Muchos a Muchos entre AppUser y Permission.
    /// </summary>
    [Table("UserPermissions")]
    public class UserPermission
    {
        // Llave foránea para AppUser
        [Column("user_id")]
        public int UserId { get; set; }

        // Llave foránea para Permission
        [Column("permission_id")]
        public int PermissionId { get; set; }

        // --- Propiedades de Navegación ---
        // Permiten acceder a los objetos relacionados directamente desde esta entidad.
        public virtual AppUser User { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
