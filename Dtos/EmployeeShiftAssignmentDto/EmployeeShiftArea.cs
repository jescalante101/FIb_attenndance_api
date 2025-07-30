using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.EmployeeShiftAssignmentDto
{
    public class EmployeeShiftArea
    {
        public int Id { get; set; }
        public string NroDoc { get; set; }
        public string FullNameEmployee { get; set; }
        public string Alias { get; set; }
        public string AreaId { get; set; }
        public string AreaName { get; set; }
        // tiempo de salida
        public List<HorarioT> Horarios { get; set; } = new List<HorarioT>();
    }
    public class HorarioT
    {
        public string NameHora { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
    }
}
