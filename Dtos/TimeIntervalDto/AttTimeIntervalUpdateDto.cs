using System;
using System.ComponentModel.DataAnnotations;

namespace Dtos.TimeIntervalDto
{
    public class AttTimeIntervalUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Alias { get; set; }

        public short UseMode { get; set; }
        public DateTime InTime { get; set; }
        public int Duration { get; set; }
        public int WorkTimeDuration { get; set; }
        public double WorkDay { get; set; }

        [StringLength(15)]
        public string? CompanyId { get; set; }

        // Punching Rules
        public short InRequired { get; set; }
        public short OutRequired { get; set; }
        public int AllowLate { get; set; }
        public int AllowLeaveEarly { get; set; }
        public short? TotalMarkings { get; set; }

        // Overtime Rules
        public short EarlyIn { get; set; }
        public short LateOut { get; set; }
        public int MinEarlyIn { get; set; }
        public int MinLateOut { get; set; }
        public short OvertimeLv { get; set; }

        public short OvertimeLv1 { get; set; }
        public decimal? OvertimeLv1Percentage { get; set; }
        public short OvertimeLv2 { get; set; }
        public decimal? OvertimeLv2Percentage { get; set; }
        public short OvertimeLv3 { get; set; }
        public decimal? OvertimeLv3Percentage { get; set; }

        public string? PunchInStartTime { get; set; }  // Ej: "07:00"
        public string? PunchInEndTime { get; set; }    // Ej: "10:00"
        public string? PunchOutStartTime { get; set; } // Ej: "16:00"
        public string? PunchOutEndTime { get; set; }   // Ej: "20:00"

        public int? RoundingThresholdMinutes { get; set; }



        public List<int> BreakTimeIds { get; set; } = new List<int>();
    }
}