using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Shifts
{
    // EmployeeScheduleException Entity
    [Table("employee_schedule_exceptions")]
    public class EmployeeScheduleException
    {
        [Key]
        [Column("exception_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExceptionId { get; set; }

        [Column("employee_id")]
        [StringLength(20)]
        [Required]
        public string EmployeeId { get; set; } = string.Empty;

        [Column("assignment_id")]
        public int? AssignmentId { get; set; }

        [Column("exception_date")]
        public DateTime? ExceptionDate { get; set; }

        [Column("day_index")]
        public int? DayIndex { get; set; }

        [Column("time_interval_id")]
        [Required]
        public int TimeIntervalId { get; set; }

        [Column("exception_type")]
        public byte? ExceptionType { get; set; } = 1;

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("remarks")]
        [StringLength(100)]
        public string? Remarks { get; set; }

        [Column("is_active")]
        public byte? IsActive { get; set; } = 1;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("created_by")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("TimeIntervalId")]
        public virtual AttTimeinterval? TimeInterval { get; set; }
    }

}
