using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.EmployeeShiftAssignmentDto
{
    public class EmployeeShiftAssignmentDTO 
    {
        public int AssignmentId { get; set; }
        public string EmployeeId { get; set; }
        public string? FullNameEmployee { get; set; }
        public int ScheduleId { get; set; }
        public string? ScheduleName { get; set; }     // <-- Cambiado de ShiftName a ScheduleName
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedWeek { get; set; }
        public string NroDoc { get; set; }
        public string? AreaId { get; set; }
        public string AreaName { get; set; }
        public string? LocationId { get; set; }
        public string? LocationName { get; set; }  // Store the name of the location
    }
}
