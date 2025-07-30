using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Manager
{
    // AppUserSite Entity
    [Table("AppUserSite")]
    public class AppUserSite
    {
        [Key, Column("userId",Order = 0)]
        public int UserId { get; set; }

        [Key, Column( "siteId",Order = 1)]
        [StringLength(30)]
        public string SiteId { get; set; } = string.Empty;

        [Column("observation")]
        [StringLength(150)]
        public string? Observation { get; set; }

        [Column("userName")]
        [StringLength(50)]
        public string? UserName { get; set; }

        [Column("siteName")]
        [StringLength(100)]
        public string? SiteName { get; set; }

        [Column("createdBy")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Column("creationDate")]
        public DateTime? CreationDate { get; set; }

        [Column("active")]
        [StringLength(1)]
        public string? Active { get; set; } = "Y";

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }
}
