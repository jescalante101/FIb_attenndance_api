using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Personal
{
    /// <summary>
    /// Entity class that maps to the 'personnel_whitelist' table in the database.
    /// This class is designed for use with Entity Framework.
    /// </summary>
    [Table("personnel_whitelist")]
    public class PersonnelWhitelist
    {
        /// <summary>
        /// Primary Key, maps to the 'id' column.
        /// It's an identity column, so EF expects the DB to generate the value on insert.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Maps to the 'employee_id' column.
        /// It's a required field with a max length of 15.
        /// </summary>
        [Required(ErrorMessage = "Employee ID is required.")]
        [StringLength(15)]
        [Column("employee_id")]
        public string EmployeeId { get; set; }

        /// <summary>
        /// Maps to the 'employee_name' column.
        /// It's a required field with a max length of 200.
        /// </summary>
        [Required(ErrorMessage = "Employee Name is required.")]
        [StringLength(200)]
        [Column("employee_name")]
        public string EmployeeName { get; set; }

        /// <summary>
        /// Maps to the 'position' column. Optional field.
        /// </summary>
        [StringLength(100)]
        [Column("position")]
        public string Position { get; set; }

        /// <summary>
        /// Maps to the 'remarks' column (VARCHAR(MAX)).
        /// </summary>
        [Column("remarks")]
        public string Remarks { get; set; }

        /// <summary>
        /// Maps to the 'created_by' column.
        /// </summary>
        [StringLength(100)]
        [Column("created_by")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Maps to the 'created_at' column.
        /// The database sets this value on creation (DEFAULT GETDATE()), so we mark it as computed.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Maps to the 'updated_by' column.
        /// </summary>
        [StringLength(100)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Maps to the 'updated_at' column.
        /// This is nullable and the database updates it via a trigger, so it's marked as computed.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
