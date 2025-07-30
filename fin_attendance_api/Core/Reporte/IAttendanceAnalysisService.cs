using Dtos.Reportes.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Reporte
{
    public interface IAttendanceAnalysisService
    {
        /// <summary>
        /// Obtiene el análisis de asistencia sin paginación (método original)
        /// </summary>
        /// <param name="request">Parámetros de búsqueda básicos</param>
        /// <returns>Lista completa de registros de análisis de asistencia</returns>
        Task<IEnumerable<AttendanceAnalysisResultDto>> GetAttendanceAnalysisAsync(
            AttendanceAnalysisRequestDto request);

        /// <summary>
        /// Obtiene el análisis de asistencia con paginación
        /// </summary>
        /// <param name="request">Parámetros de búsqueda con información de paginación</param>
        /// <returns>Resultado paginado con datos y metadatos de paginación</returns>
        Task<PaginatedAttendanceResult> GetAttendanceAnalysisPaginatedAsync(
            PaginatedAttendanceAnalysisRequestDto request);

        /// <summary>
        /// Obtiene el resumen de asistencia agrupado por empleado
        /// </summary>
        /// <param name="request">Parámetros de búsqueda básicos</param>
        /// <returns>Lista de resúmenes de asistencia por empleado</returns>
        Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(
            AttendanceAnalysisRequestDto request);
    }
}
