using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.ManualLog
{
    // AttManuallog Entity
    [Table("att_manuallog")]
    public class AttManuallog
    {
        [Key]
        [Column("manuallog_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ManuallogId { get; set; }

        [Column("abstractexception_ptr_id")]
        [Required]
        public int AbstractexceptionPtrId { get; set; }

        [Column("punch_time")]
        [Required]
        public DateTime PunchTime { get; set; }

        [Column("punch_state")]
        [Required]
        public int PunchState { get; set; }

        [Column("work_code")]
        [StringLength(20)]
        public string? WorkCode { get; set; }

        [Column("apply_reason")]
        public string? ApplyReason { get; set; }

        [Column("apply_time")]
        [Required]
        public DateTime ApplyTime { get; set; }

        [Column("audit_reason")]
        public string? AuditReason { get; set; }

        [Column("audit_time")]
        [Required]
        public DateTime AuditTime { get; set; }

        [Column("approval_level")]
        public short? ApprovalLevel { get; set; }

        [Column("audit_user_id")]
        public int? AuditUserId { get; set; }

        [Column("approver")]
        [StringLength(50)]
        public string? Approver { get; set; }

        [Column("employee_id")]
        public int? EmployeeId { get; set; }

        [Column("is_mask")]
        [Required]
        public bool IsMask { get; set; }

        [Column("temperature", TypeName = "numeric(4,1)")]
        public decimal? Temperature { get; set; }

        [Column("nro_doc")]
        [StringLength(20)]
        public string? NroDoc { get; set; }
    }
}
