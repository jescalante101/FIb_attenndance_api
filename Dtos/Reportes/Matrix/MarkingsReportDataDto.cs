using System;
using System.Collections.Generic;

namespace Dtos.Reportes.Matrix
{
    public class MarkingsReportDataDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public MarkingsReportContentDto Content { get; set; } = new();
    }

    public class MarkingsReportContentDto
    {
        public string Title { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<WeekGroupDto> WeekGroups { get; set; } = new();
        public List<MarkingsEmployeeDto> Employees { get; set; } = new();
        public MarkingsSummaryDto Summary { get; set; } = new();
    }

    public class MarkingsEmployeeDto
    {
        public int ItemNumber { get; set; }
        public string PersonalId { get; set; }
        public string NroDoc { get; set; }
        public string Colaborador { get; set; }
        public string Area { get; set; }
        public string Cargo { get; set; }
        public string FechaIngreso { get; set; }
        public string Planilla { get; set; }
        public List<MarkingsWeekDataDto> WeekData { get; set; } = new();
    }

    public class MarkingsWeekDataDto
    {
        public int WeekNumber { get; set; }
        public string Turno { get; set; }
        public List<MarkingsDayValueDto> DayValues { get; set; } = new();
    }

    public class MarkingsDayValueDto
    {
        public DateTime Date { get; set; }
        public string Value { get; set; } // "2", "4", "F", "VA", etc.
        public string DisplayValue { get; set; }
        public string Type { get; set; } // "markings", "absence", "empty"
        public int? MarkingsCount { get; set; } // Número de marcaciones cuando es tipo "markings"
        public string RawMarkings { get; set; } // String original de marcaciones para debug
        public string SpecificPermissionType { get; set; } // Tipo específico de permiso de BD: "VACACIONES", "DESCANSO MEDICO", etc.
    }

    public class MarkingsSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalPermissions { get; set; }
        public decimal AverageMarkingsPerDay { get; set; }
        public Dictionary<string, int> ConceptCounts { get; set; } = new(); // "VA": 5, "F": 10, etc.
        public Dictionary<int, int> MarkingsDistribution { get; set; } = new(); // 2: 50, 4: 30 (cantidad de días con X marcaciones)
    }
}