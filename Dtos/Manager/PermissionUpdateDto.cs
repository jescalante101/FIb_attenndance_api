using System.ComponentModel.DataAnnotations;

namespace Dtos.Manager
{
    public class PermissionUpdateDto
    {
        // PermissionId is not needed here as it's in the route
        [StringLength(100)]
        public string? PermissionKey { get; set; } // Nullable for partial updates

        [StringLength(50)]
        public string? PermissionName { get; set; } // Nullable for partial updates

        [StringLength(255)]
        public string? Description { get; set; }
    }
}