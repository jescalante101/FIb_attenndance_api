using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Reportes.Simple
{
    public class AttendanceAnalysisRequestDto
    {
        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public string? EmployeeId { get; set; }

        public string? AreaId { get; set; }

        public string? LocationId { get; set; }
    }

    public class AttendanceAnalysisResultDto
    {
        public string? NroDoc { get; set; }
        public string? EmployeeId { get; set; }
        public string? FullNameEmployee { get; set; }
        public string? AreaDescription { get; set; }
        public string? LocationName { get; set; }
        public string? AreaId { get; set; }
        public string? LocationId { get; set; }
        public DateTime Fecha { get; set; }
        public string? ShiftName { get; set; }
        public string? IntervalAlias { get; set; }
        public string? TipoMarcacion { get; set; }
        public string? TipoHorario { get; set; }
        public string? ExceptionRemarks { get; set; }
        public string? TipoPermiso { get; set; }
        public string? DiasInfo { get; set; }
        public string? HoraEsperada { get; set; }
        public string? HoraMarcacionReal { get; set; }
        public int? DiferenciaMinutos { get; set; }
        public int MinutosTardanza { get; set; }
        public int MinutosAdelanto { get; set; }
        public string? EstadoMarcacion { get; set; }
        public string? OrigenMarcacion { get; set; }
        public string? InformacionAdicional { get; set; }
        public string? RazonManual { get; set; }
        public string? ValidacionRango { get; set; }
    }

    // DTO para filtros adicionales si necesitas
    public class AttendanceFilterDto
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<string>? EmployeeIds { get; set; }
        public List<string>? AreaIds { get; set; }
        public List<string>? LocationIds { get; set; }
        public List<string>? EstadosMarcacion { get; set; }
        public string? TipoPermiso { get; set; }
    }

    // DTO para resumen de asistencia
    public class AttendanceSummaryDto
    {
        public string? EmployeeId { get; set; }
        public string? FullNameEmployee { get; set; }
        public string? AreaDescription { get; set; }
        public string? LocationName { get; set; }
        public int TotalDias { get; set; }
        public int DiasPresente { get; set; }
        public int DiasFalta { get; set; }
        public int DiasVacaciones { get; set; }
        public int DiasPermiso { get; set; }
        public int TotalMinutosTardanza { get; set; }
        public double PromedioTardanza { get; set; }
        public int DiasTardanza { get; set; }
        public double PorcentajeAsistencia { get; set; }
    }

    public class PaginatedAttendanceResult
    {
        public IEnumerable<AttendanceAnalysisResultDto> Data { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    // DTO para la solicitud con paginación
    public class PaginatedAttendanceAnalysisRequestDto : AttendanceAnalysisRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

}
