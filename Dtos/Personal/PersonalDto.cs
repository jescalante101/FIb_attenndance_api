using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Personal
{
    public class PersonalDto
    {
        public int Id { get; set; }
        public string PersonalId { get; set; }
        public string FullName { get; set; }
        public string BranchId { get; set; }
        public string BranchDescription { get; set; }
        public string AreaId { get; set; }
        public string AreaDescription { get; set; }
        public string CostCenterId { get; set; }
        public string CostCenterDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Observation { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CompanyId { get; set; }

        public int? CreatedById { get; set; }
        public string ApprovalStatus { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

    }
}
