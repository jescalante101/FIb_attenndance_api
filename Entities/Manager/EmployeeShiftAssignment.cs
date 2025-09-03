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
        [Key]
        [Required]
        [Column("assignment_id", TypeName = "int")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentId { get; set; }

        // employee_id (VARCHAR(20), NULLABLE)
        [Column("employee_id", TypeName = "varchar(20)")]
        [StringLength(20)]
        public string? EmployeeId { get; set; }

        // shift_id (INT, NOT NULL)
        [Required]
        [Column("shift_id", TypeName = "int")]
        public int ShiftId { get; set; }

        // start_date (DATE, NOT NULL)
        [Required]
        [Column("start_date", TypeName = "date")]
        public DateTime StartDate { get; set; }

        // end_date (DATE, NULLABLE)
        [Column("end_date", TypeName = "date")]
        public DateTime? EndDate { get; set; }

        // remarks (VARCHAR(255), NULLABLE)
        [StringLength(255)]
        [Column("remarks", TypeName = "varchar(255)")]
        public string? Remarks { get; set; }

        // created_at (DATETIME, NOT NULL)
        [Required]
        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        // created_by (VARCHAR(50), NULLABLE)
        [StringLength(50)]
        [Column("created_by", TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

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

        // =============================================
        // == NUEVAS PROPIEDADES AÑADIDAS ==
        // =============================================
        [StringLength(30)]
        [Column("ccost_id", TypeName = "varchar(30)")]
        public string? CcostId { get; set; }

        [StringLength(70)]
        [Column("ccost_description", TypeName = "varchar(70)")]
        public string? CcostDescription { get; set; }

        [StringLength(30)]
        [Column("compania_id", TypeName = "varchar(30)")]
        public string? CompaniaId { get; set; }
    }
}