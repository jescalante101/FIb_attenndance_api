using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Transactions
{
    public class IclockTransactionDto
    {
        public string NroDoc { get; set; }
        public string Nombres { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string NombreCompleto => $"{Nombres} {ApellidoPaterno} {ApellidoMaterno}";
        public string AreaDescripcion { get; set; }
        public string CcostoDescripcion { get; set; }
        public string TurnoAlias { get; set; }
        public string TerminalAlias { get; set; }
        public DateTime PunchTime { get; set; }
        public string HorarioAlias { get; set; }
        public TimeSpan InTime { get; set; }
        public TimeSpan OutTime { get; set; }
        public int DiaSemana { get; set; }
        public string DiaSemanaTexto => GetDiaSemanaTexto(DiaSemana);

        private string GetDiaSemanaTexto(int dia)
        {
            return dia switch
            {
                0 => "Domingo",
                1 => "Lunes",
                2 => "Martes",
                3 => "Miércoles",
                4 => "Jueves",
                5 => "Viernes",
                6 => "Sábado",
                _ => "Desconocido"
            };
        }
    }

    // DTO interno para la consulta SQL
    public class MarcacionBase
    {
        public string Nro_Doc { get; set; }
        public string Nombres { get; set; }
        public string Apellido_Paterno { get; set; }
        public string Apellido_Materno { get; set; }
        public string Personal_Id { get; set; }
        public string Area_Id { get; set; }
        public string Ccosto_Id { get; set; }
        public string terminal_alias { get; set; }
        public DateTime punch_time { get; set; }
        public string emp_code { get; set; }
        public int DiaSemana { get; set; }
    }

    public  class AsignacionTurno
    {
        public string EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string TurnoAlias { get; set; }
        public int? DayIndex { get; set; }
        public string HorarioAlias { get; set; }
        public DateTime? InTime { get; set; }
        public int? WorkTimeDuration { get; set; }
    }

    public  class IclockTransactionDtoWithTotal
    {
        public string Nro_Doc { get; set; }
        public string Nombres { get; set; }
        public string Apellido_Paterno { get; set; }
        public string Apellido_Materno { get; set; }
        public string AreaDescripcion { get; set; }
        public string CcostoDescripcion { get; set; }
        public string TurnoAlias { get; set; }
        public string TerminalAlias { get; set; }
        public DateTime PunchTime { get; set; }
        public string HorarioAlias { get; set; }
        public TimeSpan InTime { get; set; }
        public TimeSpan OutTime { get; set; }
        public int DiaSemana { get; set; }
        public int TotalRecords { get; set; }
    }

    // Clase para resultados paginados
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
