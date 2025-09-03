using System.ComponentModel.DataAnnotations;

namespace Dtos.Manager
{
    public class PermissionCreateDto
    {
        [Required]
        [StringLength(100)]
        public string PermissionKey { get; set; }

        [Required]
        [StringLength(50)]
        public string PermissionName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

    }
}