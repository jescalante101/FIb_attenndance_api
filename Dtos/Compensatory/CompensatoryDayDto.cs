using System;

namespace Dtos.Compensatory
{
    public class CompensatoryDayDto
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public int AssignmentId { get; set; }
        public DateTime HolidayWorkedDate { get; set; }
        public DateTime CompensatoryDayOffDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CompanyId { get; set; }
        public string? EmployeeFullName { get; set; }
        public string? EmployeeArea { get; set; }
        public string? EmployeeLocation { get; set; }
    }
}
