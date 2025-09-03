using System;
using System.Collections.Generic;

namespace Dtos.Reportes.Matrix
{
    public class CostCenterReportDataDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public CostCenterReportContentDto Content { get; set; } = new();
    }

    public class CostCenterReportContentDto
    {
        public string Title { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<WeekGroupDto> WeekGroups { get; set; } = new();
        public List<CostCenterEmployeeDto> Employees { get; set; } = new();
        public CostCenterSummaryDto Summary { get; set; } = new();
    }

    public class WeekGroupDto
    {
        public int WeekNumber { get; set; }
        public List<DateTime> Dates { get; set; } = new();
        public List<string> DayNames { get; set; } = new();
        public List<string> DayNumbers { get; set; } = new();
    }

    public class CostCenterEmployeeDto
    {
        public int ItemNumber { get; set; }
        public string PersonalId { get; set; }
        public string NroDoc { get; set; }
        public string Colaborador { get; set; }
        public string Area { get; set; }
        public string Cargo { get; set; }
        public string FechaIngreso { get; set; }
        public string Planilla { get; set; }
        public List<CostCenterWeekDataDto> WeekData { get; set; } = new();
    }

    public class CostCenterWeekDataDto
    {
        public int WeekNumber { get; set; }
        public string Turno { get; set; }
        public List<CostCenterDayValueDto> DayValues { get; set; } = new();
    }

    public class CostCenterDayValueDto
    {
        public DateTime Date { get; set; }
        public string Value { get; set; } // CC Código, "F" (Falta), "VA" (Vacaciones), etc.
        public string DisplayValue { get; set; }
        public string Type { get; set; } // "work", "absence", "empty" 
        public string SpecificPermissionType { get; set; } // Tipo específico de permiso de BD: "VACACIONES", "DESCANSO MEDICO", etc.
    }

    public class CostCenterSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalPermissions { get; set; }
        public Dictionary<string, int> ConceptCounts { get; set; } = new(); // "VA": 5, "F": 10, etc.
    }
}