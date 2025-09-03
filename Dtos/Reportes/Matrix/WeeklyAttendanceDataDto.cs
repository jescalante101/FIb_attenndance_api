using System;
using System.Collections.Generic;

namespace Dtos.Reportes.Matrix
{
    public class WeeklyAttendanceDataDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public WeeklyAttendanceContentDto Content { get; set; } = new();
    }

    public class WeeklyAttendanceContentDto
    {
        public string Title { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<WeekGroupDto> WeekGroups { get; set; } = new();
        public List<WeeklyAttendanceEmployeeDto> Employees { get; set; } = new();
        public WeeklyAttendanceSummaryDto Summary { get; set; } = new();
    }

    public class WeeklyAttendanceEmployeeDto
    {
        public int ItemNumber { get; set; }
        public string PersonalId { get; set; }
        public string NroDoc { get; set; }
        public string Colaborador { get; set; }
        public string Area { get; set; }
        public string CentroCosto { get; set; }
        public string Cargo { get; set; }
        public string FechaIngreso { get; set; }
        public string Planilla { get; set; }
        public List<WeeklyAttendanceWeekDataDto> WeekData { get; set; } = new();
        public WeeklyAttendanceGlobalTotalsDto GlobalTotals { get; set; } = new();
    }

    public class WeeklyAttendanceWeekDataDto
    {
        public int WeekNumber { get; set; }
        public string Turno { get; set; } // Turno predominante de la semana
        public List<WeeklyAttendanceDayDataDto> DayData { get; set; } = new();
        public WeeklyAttendanceWeekTotalsDto WeekTotals { get; set; } = new();
    }

    public class WeeklyAttendanceDayDataDto
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public string EntradaReal { get; set; }
        public string SalidaReal { get; set; }
        public decimal HorasTrabjadas { get; set; }
        public string Type { get; set; } // "work", "absence", "permission", "empty"
        public string TipoPermiso { get; set; }
        public string EstadoColor { get; set; } // Para frontend: "success", "danger", "warning", "secondary"
    }

    public class WeeklyAttendanceWeekTotalsDto
    {
        public decimal HorasTrabajadas { get; set; }
        public decimal HorasExtras { get; set; }
    }

    public class WeeklyAttendanceGlobalTotalsDto
    {
        public decimal TotalHoras { get; set; }
        public decimal TotalExtras { get; set; }
    }

    public class WeeklyAttendanceSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalPermissions { get; set; }
        public decimal TotalHorasGlobales { get; set; }
        public decimal TotalHorasExtrasGlobales { get; set; }
        public decimal AverageHoursPerEmployee { get; set; }
        public decimal AverageOvertimePerEmployee { get; set; }
        public Dictionary<string, int> ConceptCounts { get; set; } = new(); // "VA": 5, "F": 10, etc.
        public List<WeeklyAttendanceWeekSummaryDto> WeekSummaries { get; set; } = new();
    }

    public class WeeklyAttendanceWeekSummaryDto
    {
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalHorasSemana { get; set; }
        public decimal TotalExtrasSemana { get; set; }
        public int EmpleadosConDatos { get; set; }
    }
}