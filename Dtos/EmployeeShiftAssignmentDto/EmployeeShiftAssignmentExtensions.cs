using Entities.Manager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.EmployeeShiftAssignmentDto
{
    /// <summary>
    /// Extension methods para conversión entre DTO y Entity
    /// </summary>
    public static class EmployeeShiftAssignmentExtensions
    {
        /// <summary>
        /// Convierte una lista de DTOs a entidades
        /// </summary>
        /// <param name="dtos">Lista de DTOs</param>
        /// <returns>Lista de entidades</returns>
        public static List<EmployeeShiftAssignment> ToEntities(this List<CreateEmployeeShiftAssignmentDTO> dtos)
        {
            return dtos.Select(dto => dto.ToEntity()).ToList();
        }

        /// <summary>
        /// Convierte una entidad a DTO de respuesta
        /// </summary>
        /// <param name="entity">Entidad</param>
        /// <returns>DTO de respuesta</returns>
        public static EmployeeShiftAssignmentDTO ToResponseDTO(this EmployeeShiftAssignment entity)
        {
            return new EmployeeShiftAssignmentDTO
            {
                AssignmentId = entity.AssignmentId,
                EmployeeId = entity.EmployeeId,
                FullNameEmployee = entity.FullNameEmployee,
                ScheduleName = entity.ShiftDescription,
                ScheduleId = entity.ShiftId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Remarks = entity.Remarks,
                CreatedAt = entity.CreatedAt,
                NroDoc = entity.NroDoc ?? "-",
                AreaId = entity.AreaId,
                AreaName = entity.AreaDescription ?? "-",
                CreatedWeek = ISOWeek.GetWeekOfYear(entity.CreatedAt),
                LocationId = entity.LocationId,
                LocationName = entity.LocationName ?? "-"
            };
        }
    }
}
