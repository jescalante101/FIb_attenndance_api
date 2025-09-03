using System.ComponentModel.DataAnnotations;

namespace Dtos.Compensatory
{
    public class CreateCompensatoryDayDto
    {
        [Required]
        [StringLength(20)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public DateTime HolidayWorkedDate { get; set; }

        [Required]
        public DateTime CompensatoryDayOffDate { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [StringLength(10)]
        public string? CompanyId { get; set; }
    }
}
