using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.ShiftDto
{
    public class ScheduleResponseDto
    {
        public string EmployeeId { get; set; }
        public string FullNameEmployee { get; set; }
        public int AssignmentId { get; set; }
        public ShiftDto ShiftInfo { get; set; }
        public DateRangeDto QueryRange { get; set; }
        public List<ScheduleDayDto> Schedule { get; set; }
    }

    // Información básica del turno
    public class ShiftDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public int ShiftCycle { get; set; } // Duración del ciclo en días
    }

    // Representa un día específico en el horario final
    public class ScheduleDayDto
    {
        public DateTime Date { get; set; }
        public int ScheduleId { get; set; } // ID del horario asociado
        public string DayName { get; set; }
        public string Alias { get; set; } // Nombre del horario (ej: "Turno Mañana")
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public int WorkTimeDurationMinutes { get; set; }
        public int Duration { get; set; }
        public bool IsException { get; set; }
    }

    // DTO interno para el horario base del turno
    public class ShiftBaseScheduleDto
    {
        public int DayIndex { get; set; } // El día dentro del ciclo (0, 1, 2...)
        public string Alias { get; set; }
        public DateTime InTime { get; set; }
        public int WorkTimeDurationMinutes { get; set; }
        public int Id { get; set; } // ID del horario base
        public int Duration { get; set; } // <--- CAMPO AÑADIDO
    }

    // DTO para las excepciones
    public class ExceptionDto
    {
        public DateTime ExceptionDate { get; set; }
        public string Alias { get; set; }
        public DateTime InTime { get; set; }
        
        public int ExceptionId { get; set; } // ID de la excepción
        public int WorkTimeDurationMinutes { get; set; }
        public int Duration { get; set; } // <--- CAMPO AÑADIDO
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


}
