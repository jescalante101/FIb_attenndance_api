using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Reportes.HorasExtras
{
    // AsistenciaDiariaSpDto.cs
    public class AsistenciaDiariaSpDto
    {
        public string Nro_Doc { get; set; }
        public string Colaborador { get; set; }
        public string Area { get; set; }
        public string Sede { get; set; }
        public string Cargo { get; set; }
        public string TipoTurno { get; set; }
        public DateTime Fecha { get; set; }
        public string HoraEntrada { get; set; }
        public string HoraSalida { get; set; }
        public int MinutosTardanza { get; set; }
        public int TotalMinutosPagados { get; set; }
        public int HorasNormales { get; set; }
        public int HorasExtrasNivel1 { get; set; }
        public int HorasExtrasNivel2 { get; set; }
        public int HorasExtras100 { get; set; }
    }



    ///reporte
    ///

    // DTO para los datos de un solo día en el reporte
    public class AsistenciaDiaReporteDto
    {
        public string HoraEntrada { get; set; }
        public string HoraSalida { get; set; }
        public double HorasNormales { get; set; }
        public double HorasExtras1 { get; set; }
        public double HorasExtras2 { get; set; }
        public double HorasExtras100 { get; set; }
        public string Estado { get; set; } // Para "FALTA", "VACACIONES", etc.
        public string TipoTurno { get; set; }
    }

    // DTO principal que representa una fila completa del reporte para un empleado
    public class ReporteAsistenciaSemanalDto
    {
        public string Nro_Doc { get; set; }
        public string Colaborador { get; set; }
        public string Area { get; set; }
        public string Sede { get; set; }
        public string Cargo { get; set; }
        public DateTime? FechaIngreso { get; set; }

        // Un diccionario para acceder fácilmente a los datos de cada día por fecha específica
        public Dictionary<DateTime, AsistenciaDiaReporteDto> AsistenciaPorDia { get; set; } = new Dictionary<DateTime, AsistenciaDiaReporteDto>();

        // Totales de la semana
        public double TotalHorasNormales { get; set; }
        public double TotalHorasExtras1 { get; set; }
        public double TotalHorasExtras2 { get; set; }
        public double TotalHorasExtras100 { get; set; }
        public int TotalVacaciones { get; set; }
        public int TotalFaltas { get; set; }
        public int TotalPermisos { get; set; }
    }


    public class ReportFiltersHE
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CompanyId { get; set; }
        public string? EmployeeIds { get; set; } // Lista de IDs como "id1,id2,id3"
        public string? AreaId { get; set; }
        public string? SedeId { get; set; }
    }
}
