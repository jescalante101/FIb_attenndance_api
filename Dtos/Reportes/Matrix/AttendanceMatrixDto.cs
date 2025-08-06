using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Reportes.Matrix
{
    public class AttendanceMatrixDto
    {
        public string PersonalId { get; set; }
        public string NroDoc { get; set; }
        public string Colaborador { get; set; }
        public string Sede { get; set; }
        public string SedeCodigo { get; set; }
        public string Area { get; set; }
        public string Cargo { get; set; }
        public string CentroCosto { get; set; }
        public string CcCodigo { get; set; }
        public string Compania { get; set; }
        public string FechaIngreso { get; set; }

        // Información del día
        public DateTime Fecha { get; set; }
        public string DiaSemanaEs { get; set; }

        // Configuración de horario
        public string TurnoNombre { get; set; }
        public string TipoHorario { get; set; }
        public string TipoDia { get; set; }

        // Horarios programados
        public string EntradaProgramada { get; set; }
        public string SalidaProgramada { get; set; }
        public int? MarcacionesEsperadas { get; set; }
        public string BreaksConfigurados { get; set; }

        // Permisos
        public string TipoPermiso { get; set; }

        // Marcaciones reales - NUEVOS CAMPOS AGREGADOS
        public string MarcacionesDelDia { get; set; }
        public string MarcacionesManuales { get; set; }
        public string RazonesManuales { get; set; }
        public string OrigenMarcaciones { get; set; }

        // datos de paginación
        public int? TotalRecords { get; set; }
        public int? CurrentPage { get; set; }
        public int? PageSize { get; set; } 
        public double? TotalPages { get; set; }
    }


    public class AttendanceMatrixFilterDto
    {
        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public string? EmployeeId { get; set; }
        public string? CompaniaId { get; set; }
        public string? AreaId { get; set; }
        public string? SedeId { get; set; }
        public string? CargoId { get; set; }
        public string? CentroCostoId { get; set; }
        public string? SedeCodigo { get; set; }
        public string? CcCodigo { get; set; }
        public string ? PlanillaId { get; set; } // Nuevo campo para filtrar por planilla 
        // opciones para la paginación
        public  int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;


    }




    /// <summary>
    /// DTO para respuesta del servicio
    /// </summary>
    public class AttendanceMatrixResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRecords { get; set; }
        public double TotalPages { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }

        public List<AttendanceMatrixDto> Data { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

}
