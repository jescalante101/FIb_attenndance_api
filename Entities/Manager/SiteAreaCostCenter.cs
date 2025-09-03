using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Manager
{
    // SiteAreaCostCenter Entity
    [Table("SiteAreaCostCenter")]
    public class SiteAreaCostCenter
    {
        [Key, Column("siteId", Order = 0)]
        [StringLength(30)]
        public string SiteId { get; set; } = string.Empty;

        [Key, Column("areaId", Order = 1)]
        [StringLength(30)]
        public string AreaId { get; set; } = string.Empty;

        [Column("observation")]
        [StringLength(150)]
        public string? Observation { get; set; }

        [Column("siteName")]
        [StringLength(100)]
        public string? SiteName { get; set; }

        [Column("areaName")]
        [StringLength(100)]
        public string? AreaName { get; set; }

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

        [Column("costCenterId")]
        [StringLength(30)]
        public string? CostCenterId { get; set; }

        [Column("costCenterName")]
        [StringLength(100)]
        public string? CostCenterName { get; set; }
    }

}
