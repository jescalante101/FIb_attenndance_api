using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Personal
{
    [Table("Personal")]
    public class PersonalEntity
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("personal_id")]
        [StringLength(30)]
        [Required]
        public string PersonalId { get; set; }

        [Column("full_name")]
        [StringLength(200)]
        public string FullName { get; set; }

        [Column("branch_id")]
        [StringLength(30)]
        [Required]
        public string BranchId { get; set; }

        [Column("branch_description")]
        [StringLength(100)]
        [Required]
        public string BranchDescription { get; set; }

        [Column("area_id")]
        [StringLength(30)]
        [Required]
        public string AreaId { get; set; }

        [Column("area_description")]
        [StringLength(100)]
        [Required]
        public string AreaDescription { get; set; }

        [Column("cost_center_id")]
        [StringLength(30)]
        [Required]
        public string CostCenterId { get; set; }

        [Column("cost_center_description")]
        [StringLength(100)]
        [Required]
        public string CostCenterDescription { get; set; }

        [Column("start_date")]
        [Required]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("observation")]
        public string Observation { get; set; }

        [Column("created_by")]
        [StringLength(30)]
        [Required]
        public string CreatedBy { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        [Column("updated_by")]
        [StringLength(30)]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("company_id")]
        [StringLength(30)]
        public string? CompanyId { get; set; }

        // ==========================================================
        // ===== ✅ INICIO: NUEVOS CAMPOS AÑADIDOS ✅ =====
        // ==========================================================

        /// <summary>
        /// ID numérico del usuario que creó el registro.
        /// </summary>
        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// Estado de aprobación del registro (P=Pendiente, A=Aprobado, R=Rechazado).
        /// </summary>
        [Column("approval_status")]
        [StringLength(1)]
        [Required]
        public string ApprovalStatus { get; set; }

        /// <summary>
        /// Usuario que aprobó o rechazó el registro.
        /// </summary>
        [Column("approved_by")]
        [StringLength(30)]
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// Fecha y hora en que se aprobó o rechazó el registro.
        /// </summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        // ==========================================================
        // ===== 🔚 FIN: NUEVOS CAMPOS AÑADIDOS 🔚 =====
        // ==========================================================


        // Constructor
        public PersonalEntity()
        {
            CreatedAt = DateTime.Now;
            // Se establece 'P' (Pendiente) como estado por defecto al crear una nueva instancia.
            ApprovalStatus = "P";
        }
    }
}