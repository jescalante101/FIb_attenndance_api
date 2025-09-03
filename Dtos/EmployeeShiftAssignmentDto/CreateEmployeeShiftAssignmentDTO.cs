using Entities.Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.EmployeeShiftAssignmentDto
{
    public class CreateEmployeeShiftAssignmentDTO
    {
        // Propiedades existentes...
        [StringLength(20)]
        public string? EmployeeId { get; set; }

        [Required(ErrorMessage = "El ID del turno es obligatorio")]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "La fecha de creación es obligatoria")]
        public DateTime CreatedAt { get; set; }

        [StringLength(50)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        [StringLength(255)]
        public string? FullNameEmployee { get; set; }

        [StringLength(255)]
        public string? ShiftDescription { get; set; }

        [StringLength(20)]
        public string? NroDoc { get; set; }

        [StringLength(20)]
        public string? AreaId { get; set; }

        [StringLength(80)]
        public string? AreaDescription { get; set; }

        [StringLength(20)]
        public string? LocationId { get; set; }

        [StringLength(20)]
        public string? LocationName { get; set; }

        // =============================================
        // == NUEVAS PROPIEDADES AÑADIDAS ==
        // =============================================
        [StringLength(30)]
        public string? CcostId { get; set; }

        [StringLength(70)]
        public string? CcostDescription { get; set; }

        [StringLength(30)]
        public string? CompaniaId { get; set; }

        /// <summary>
        /// Convierte el DTO a la entidad EmployeeShiftAssignment
        /// </summary>
        /// <returns>Nueva instancia de EmployeeShiftAssignment</returns>
        public EmployeeShiftAssignment ToEntity()
        {
            return new EmployeeShiftAssignment
            {
                // Mapeo de propiedades existentes...
                EmployeeId = this.EmployeeId,
                ShiftId = this.ShiftId,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                Remarks = this.Remarks,
                CreatedAt = this.CreatedAt,
                CreatedBy = this.CreatedBy,
                UpdatedAt = this.UpdatedAt,
                UpdatedBy = this.UpdatedBy,
                FullNameEmployee = this.FullNameEmployee,
                ShiftDescription = this.ShiftDescription,
                NroDoc = this.NroDoc,
                AreaId = this.AreaId,
                AreaDescription = this.AreaDescription,
                LocationId = this.LocationId,
                LocationName = this.LocationName,

                // =============================================
                // == MAPEO DE NUEVAS PROPIEDADES ==
                // =============================================
                CcostId = this.CcostId,
                CcostDescription = this.CcostDescription,
                CompaniaId = this.CompaniaId
            };
        }
    }
}