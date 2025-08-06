// Core/IclockTransaction/IIclockTransactionService.cs
using Dtos.Transactions;

namespace FibAttendanceApi.Core.IclockTransaction
{
    public interface IIclockTransactionService
    {
        // ===== MÉTODOS ORIGINALES (mantener compatibilidad) =====

        /// <summary>
        /// Método original - Obtiene asistencias básicas sin paginación
        /// </summary>
        Task<List<IclockTransactionDto>> GetAsistenciasCompletasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null);

        /// <summary>
        /// Método original - Obtiene asistencias básicas con paginación
        /// </summary>
        Task<PagedResult<IclockTransactionDto>> GetAsistenciasCompletasPaginadasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null,
            int pageNumber = 1,
            int pageSize = 50);

        // ===== MÉTODOS NUEVOS CON ANÁLISIS MEJORADO =====

        /// <summary>
        /// NUEVO - Obtiene análisis completo de asistencias con lógica de proximidad sin paginación
        /// </summary>
        Task<List<AsistenciaCompletaDto>> GetAnalisisAsistenciasCompletasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null);

        /// <summary>
        /// NUEVO - Obtiene análisis completo de asistencias con lógica de proximidad con paginación
        /// </summary>
        Task<PagedResult<AsistenciaCompletaDto>> GetAnalisisAsistenciasCompletasPaginadasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null,
            int pageNumber = 1,
            int pageSize = 50);
    }
}