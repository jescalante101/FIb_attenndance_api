using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Reportes.Matrix
{
    public class AttendanceMatrixPivotResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<EmployeePivotData> Employees { get; set; }
        public List<DateTime> DateRange { get; set; }
        public AttendanceSummaryPivotDto Summary { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }

        public int? TotalRecords { get; set; }
        public int? CurrentPage { get; set; }
        public int? PageSize { get; set; }
        public double? TotalPages { get; set; }

    }

    public class AttendanceSummaryPivotDto
    {
        public int TotalEmployees { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalPermissions { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
    }
}
