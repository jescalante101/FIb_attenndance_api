using System.ComponentModel.DataAnnotations;

namespace Dtos.Compensatory
{
    public class UpdateCompensatoryDayDto
    {
        [Required]
        public DateTime CompensatoryDayOffDate { get; set; }

        [Required]
        [StringLength(1)]
        public string Status { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Remarks { get; set; }
    }
}
