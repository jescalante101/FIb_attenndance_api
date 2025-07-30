using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.ManuaLog
{
    public class AttManuallogDto
    {
        public int ManualLogId { get; set; }
        public int AbstractexceptionPtrId { get; set; }
        public DateTime PunchTime { get; set; }
        public int PunchState { get; set; }
        public string? WorkCode { get; set; }
        public string? ApplyReason { get; set; }
        public DateTime ApplyTime { get; set; }
        public string? AuditReason { get; set; }
        public DateTime AuditTime { get; set; }
        public short? ApprovalLevel { get; set; }
        public int? AuditUserId { get; set; }
        public string? Approver { get; set; }
        public int EmployeeId { get; set; }
        public bool IsMask { get; set; }
        public decimal? Temperature { get; set; }
        public string? NroDoc { get; set; }
    }

    public class AttManuallogCreateDto
    {
        public DateTime PunchTime { get; set; }
        public int PunchState { get; set; }
        public string? WorkCode { get; set; }
        public string? ApplyReason { get; set; }
        public DateTime ApplyTime { get; set; }
        public string? AuditReason { get; set; }
        public DateTime AuditTime { get; set; }
        public short? ApprovalLevel { get; set; }
        public int? AuditUserId { get; set; }
        public string? Approver { get; set; }
        public int EmployeeId { get; set; }
        public bool IsMask { get; set; }
        public decimal? Temperature { get; set; }
        public string? NroDoc { get; set; }
    }
}
