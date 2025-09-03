using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Personal
{
    public class CreatePersonalDto
    {
        [Required]
        [StringLength(30)]
        public string PersonalId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(30)]
        public string BranchId { get; set; }

        [Required]
        [StringLength(100)]
        public string BranchDescription { get; set; }

        [Required]
        [StringLength(30)]
        public string AreaId { get; set; }

        [Required]
        [StringLength(100)]
        public string AreaDescription { get; set; }

        [Required]
        [StringLength(30)]
        public string CostCenterId { get; set; }

        [Required]
        [StringLength(100)]
        public string CostCenterDescription { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Observation { get; set; }

        [Required]
        [StringLength(30)]
        public string CreatedBy { get; set; }

        [Required]
        [StringLength(30)]
        public string CompanyId { get; set; }

        [Required]
        public int CreatedById { get; set; }
    }
}
