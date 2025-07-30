using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Manager
{
    [Table("EmployeeShiftAssignments")]
    public class EmployeeShiftAssignment
    {
        // Clave primaria
        [Key] // Marca esta propiedad como la clave primaria de la entidad
        [Required] // Indica que esta columna no puede ser NULL (NO en IS_NULLABLE)
        [Column("assignment_id", TypeName = "int")] // Mapea a la columna 'assignment_id' y especifica el tipo de DB
        public int AssignmentId { get; set; }

        // employee_id (VARCHAR(20), NULLABLE)
        [Column("employee_id", TypeName = "varchar(20)")] // Mapea a 'employee_id' y especifica el tipo y longitud
        [StringLength(20)] // Establece la longitud máxima para el string
        public string? EmployeeId { get; set; } // '?' indica que es anulable (nullable)

        // shift_id (INT, NOT NULL)
        [Required]
        [Column("shift_id", TypeName = "int")]
        public int ShiftId { get; set; }

        // start_date (DATE, NOT NULL)
        [Required]
        [Column("start_date", TypeName = "date")] // Usa TypeName para mapear a tipo DATE si EF Core no lo infiere correctamente
        public DateTime StartDate { get; set; }

        // end_date (DATE, NULLABLE)
        [Column("end_date", TypeName = "date")]
        public DateTime? EndDate { get; set; } // '?' indica que es anulable

        // remarks (VARCHAR(255), NULLABLE)
        [StringLength(255)]
        [Column("remarks", TypeName = "varchar(255)")]
        public string? Remarks { get; set; }

        // created_at (DATETIME, NOT NULL)
        [Required]
        [Column("created_at", TypeName = "datetime")] // Para SQL Server puede ser 'datetime' o 'datetime2'
        public DateTime CreatedAt { get; set; }

        // created_by (VARCHAR(50), NULLABLE)
        [StringLength(50)]
        [Column("created_by", TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        // full_name_employee (VARCHAR(255), NULLABLE)
        [StringLength(255)]
        [Column("full_name_employee", TypeName = "varchar(255)")]
        public string? FullNameEmployee { get; set; }

        // shift_description (VARCHAR(255), NULLABLE)
        [StringLength(255)]
        [Column("shift_description", TypeName = "varchar(255)")]
        public string? ShiftDescription { get; set; }

        // nro_doc (VARCHAR(20), NULLABLE)
        [StringLength(20)]
        [Column("nro_doc", TypeName = "varchar(20)")]
        public string? NroDoc { get; set; }

        // area_id (VARCHAR(20), NULLABLE)
        [StringLength(20)]
        [Column("area_id", TypeName = "varchar(20)")]
        public string? AreaId { get; set; }

        // area_description (VARCHAR(80), NULLABLE)
        [StringLength(80)]
        [Column("area_description", TypeName = "varchar(80)")]
        public string? AreaDescription { get; set; }

        // location_id (VARCHAR(20), NULLABLE)
        [StringLength(20)]
        [Column("location_id", TypeName = "varchar(20)")]
        public string? LocationId { get; set; }

        // location_name (VARCHAR(20), NULLABLE)
        [StringLength(20)]
        [Column("location_name", TypeName = "varchar(20)")]
        public string? LocationName { get; set; }
    }
}
