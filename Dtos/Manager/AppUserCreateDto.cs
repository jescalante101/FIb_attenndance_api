
using System.ComponentModel.DataAnnotations;

namespace Dtos.Manager
{
    public class AppUserCreateDto
    {
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }
        //
        public string? CreatedBy { get; set; }
        public string? UpdateBY { get; set; }
    }
}
