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

        [Column("created_by")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        [Column("active")]
        [StringLength(1)]
        public string? Active { get; set; } = "Y";

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }
}
