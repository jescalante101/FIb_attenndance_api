using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Reportes.Matrix
{
    public class EmployeePivotData
    {
        public string PersonalId { get; set; }
        public string NroDoc { get; set; }
        public string Colaborador { get; set; }
        public string Sede { get; set; }
        public string Area { get; set; }
        public string Cargo { get; set; }
        public string CentroCosto { get; set; }
        public string CCCodigo { get; set; }
        public string Compania { get; set; }
        public string Planilla { get; set; }
        public string FechaIngreso { get; set; }
        public Dictionary<DateTime, DailyAttendanceData> DailyData { get; set; } = new();
        public decimal TotalHoras { get; set; }
        public decimal HorasExtras { get; set; }
    }

    public class DailyAttendanceData
    {
        public string DiaSemana { get; set; }
        public string TipoDia { get; set; }
        public string TurnoNombre { get; set; }
        public string EntradaProgramada { get; set; }
        public string SalidaProgramada { get; set; }
        public string MarcacionesDelDia { get; set; }
        public string OrigenMarcaciones { get; set; }
        public string TipoPermiso { get; set; }
        public string EntradaReal { get; set; }
        public string SalidaReal { get; set; }
    }

}
