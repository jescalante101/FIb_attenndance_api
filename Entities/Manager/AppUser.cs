using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Manager
{
    [Table("AppUser")]
    public class AppUser
    {
        [Key]
        [Column("userId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Column("userName")]
        [StringLength(50)]
        public string? UserName { get; set; }

        // Navigation Properties
        public virtual ICollection<AppUserSite> AppUserSites { get; set; } = new List<AppUserSite>();
    }
}
