using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Personal
{
    public class UpdatePersonalDto
    {
        [StringLength(30)]
        public string BranchId { get; set; }

        [StringLength(100)]
        public string BranchDescription { get; set; }

        [StringLength(30)]
        public string AreaId { get; set; }

        [StringLength(100)]
        public string AreaDescription { get; set; }

        [StringLength(30)]
        public string CostCenterId { get; set; }

        [StringLength(100)]
        public string CostCenterDescription { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Observation { get; set; }

        [Required]
        [StringLength(30)]
        public string UpdatedBy { get; set; }

        public string ApprovalStatus { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }
    }
}
