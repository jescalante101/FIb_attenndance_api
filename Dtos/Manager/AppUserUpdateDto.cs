
using System.ComponentModel.DataAnnotations;

namespace Dtos.Manager
{
    public class AppUserUpdateDto
    {
        [StringLength(50)]
        public string? UserName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public bool? IsActive { get; set; }

        public string? UpdatedBy { get; set; }

    }
}
