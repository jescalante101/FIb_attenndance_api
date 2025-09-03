using Dtos.BreakTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.TimeIntervalDto
{
    public class AttTimeIntervalDto
    {
        // --- Sección Principal ---
        public int Id { get; set; }
        public string Alias { get; set; }
        public short UseMode { get; set; }
        public DateTime InTime { get; set; }
        public int Duration { get; set; }
        public int WorkTimeDuration { get; set; }
        public double WorkDay { get; set; }
        public string CompaniaId { get; set; }

        // --- Sección de Reglas de Marcación ---
        public short InRequired { get; set; }
        public short OutRequired { get; set; }
        public int AllowLate { get; set; }
        public int AllowLeaveEarly { get; set; }
        public short? TotalMarkings { get; set; }

        // --- Sección de Horas Extras ---
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

        // --- Campos de Auditoría (opcional, solo para respuestas) ---
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string PunchInWindow { get; set; }  // Ej: "07:00 - 10:00"
        public string PunchOutWindow { get; set; } // Ej: "16:00 - 20:00"

        public int? RoundingThresholdMinutes { get; set; }

        public List<BreakTimeDto> Breaks { get; set; }

    }
}
