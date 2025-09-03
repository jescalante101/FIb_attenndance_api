using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.ScheduleExceptionsDto
{
    public class EmployeeScheduleExceptionCreateDto
    {
        [Required(ErrorMessage = "El ID del empleado es requerido")]
        [StringLength(20, ErrorMessage = "El ID del empleado no puede exceder 20 caracteres")]
        public string EmployeeId { get; set; }

        public int AssignmentId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExceptionDate { get; set; }

        [Range(0, 6, ErrorMessage = "El día de la semana debe estar entre 0 (domingo) y 6 (sábado)")]
        public int? DayIndex { get; set; }

        [Required(ErrorMessage = "El ID del horario es requerido")]
        public int TimeIntervalId { get; set; }

        [Range(1, 2, ErrorMessage = "El tipo de excepción debe ser 1 (fecha específica) o 2 (recurrente)")]
        public byte ExceptionType { get; set; } = 1;

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        public string Remarks { get; set; }

        [Required(ErrorMessage = "El usuario creador es requerido")]
        [StringLength(50, ErrorMessage = "El usuario creador no puede exceder 50 caracteres")]
        public string CreatedBy { get; set; }
    }

    public class EmployeeScheduleExceptionUpdateDto
    {
        public int AssignmentId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExceptionDate { get; set; }

        [Range(0, 6, ErrorMessage = "El día de la semana debe estar entre 0 (domingo) y 6 (sábado)")]
        public int? DayIndex { get; set; }

        [Required(ErrorMessage = "El ID del horario es requerido")]
        public int TimeIntervalId { get; set; }

        [Range(1, 2, ErrorMessage = "El tipo de excepción debe ser 1 (fecha específica) o 2 (recurrente)")]
        public byte ExceptionType { get; set; } = 1;

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        public string Remarks { get; set; }

        [Required(ErrorMessage = "El usuario que actualiza es requerido")]
        [StringLength(50, ErrorMessage = "El usuario que actualiza no puede exceder 50 caracteres")]
        public string UpdatedBy { get; set; }
    }

}
