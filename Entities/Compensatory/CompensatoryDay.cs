using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Manager;

namespace Entities.Compensatory
{
    [Table("CompensatoryDays")]
    public class CompensatoryDay
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime HolidayWorkedDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime CompensatoryDayOffDate { get; set; }

        [Required]
        [StringLength(1)]
        [Column(TypeName = "char")]
        public string Status { get; set; } = string.Empty;

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string? CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

                [StringLength(10)]
        [Column("company_id", TypeName = "varchar")]
        public string? CompanyId { get; set; }

        // Navigation property
        [ForeignKey("AssignmentId")]
        public EmployeeShiftAssignment? EmployeeAssignment { get; set; }
    }
}
    

