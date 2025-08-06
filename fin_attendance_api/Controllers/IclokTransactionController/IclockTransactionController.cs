using Dtos.Transactions;
using FibAttendanceApi.Core.IclockTransaction;
using Microsoft.AspNetCore.Mvc;

namespace FibAttendanceApi.Controllers.IclokTransactionController
{
    /// <summary>
    /// Controlador para obtener marcaciones y análisis de asistencias de empleados
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IclockTransactionController : ControllerBase
    {
        private readonly IIclockTransactionService _iclockTransactionService;

        public IclockTransactionController(IIclockTransactionService iclockTransactionService)
        {
            _iclockTransactionService = iclockTransactionService;
        }

        // ===== ENDPOINTS ORIGINALES (mantener compatibilidad) =====

        /// <summary>
        /// Obtiene asistencias básicas sin paginación (ORIGINAL)
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <param name="empleadoFilter">ID del empleado (opcional)</param>
        /// <returns>Lista de asistencias básicas</returns>
        [HttpGet]
        public async Task<ActionResult<List<IclockTransactionDto>>> GetAsistencias(
           [FromQuery] DateTime fechaInicio,
           [FromQuery] DateTime fechaFin,
           [FromQuery] string empleadoFilter = null)
        {
            try
            {
                var resultado = await _iclockTransactionService.GetAsistenciasCompletasAsync(
                    fechaInicio, fechaFin, empleadoFilter);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener asistencias",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene asistencias básicas con paginación (ORIGINAL)
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <param name="empleadoFilter">ID del empleado (opcional)</param>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 50, max: 1000)</param>
        /// <returns>Resultado paginado de asistencias básicas</returns>
        [HttpGet("paginado")]
        public async Task<ActionResult<PagedResult<IclockTransactionDto>>> GetAsistenciasPaginadas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string empleadoFilter = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parámetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 1000) pageSize = 1000;

                var resultado = await _iclockTransactionService.GetAsistenciasCompletasPaginadasAsync(
                    fechaInicio, fechaFin, empleadoFilter, pageNumber, pageSize);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener asistencias paginadas",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== ENDPOINTS NUEVOS CON ANÁLISIS MEJORADO =====

        /// <summary>
        /// 🆕 Obtiene análisis completo de asistencias sin paginación
        /// Incluye lógica de proximidad temporal, cálculos de tardanzas, y análisis detallado
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <param name="empleadoFilter">ID del empleado (opcional)</param>
        /// <returns>Lista completa con análisis de asistencias</returns>
        [HttpGet("analisis")]
        public async Task<ActionResult<List<AsistenciaCompletaDto>>> GetAnalisisAsistencias(
           [FromQuery] DateTime fechaInicio,
           [FromQuery] DateTime fechaFin,
           [FromQuery] string empleadoFilter = null)
        {
            try
            {
                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    return BadRequest(new
                    {
                        error = "Rango de fechas inválido",
                        message = "La fecha fin debe ser mayor o igual a la fecha inicio"
                    });
                }

                // Validar que el rango no sea muy grande (opcional - ajustar según necesidad)
                var diasRango = (fechaFin - fechaInicio).Days;
                if (diasRango > 365 && string.IsNullOrEmpty(empleadoFilter))
                {
                    return BadRequest(new
                    {
                        error = "Rango de fechas muy amplio",
                        message = "Para rangos mayores a 1 año, especifique un empleado o use paginación"
                    });
                }

                var resultado = await _iclockTransactionService.GetAnalisisAsistenciasCompletasAsync(
                    fechaInicio, fechaFin, empleadoFilter);

                return Ok(new
                {
                    data = resultado,
                    meta = new
                    {
                        totalRegistros = resultado.Count,
                        rangoFechas = new { fechaInicio, fechaFin },
                        empleadoFiltrado = !string.IsNullOrEmpty(empleadoFilter),
                        generadoEn = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener análisis de asistencias",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// 🆕 Obtiene análisis completo de asistencias con paginación
        /// Incluye lógica de proximidad temporal, cálculos de tardanzas, y análisis detallado
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <param name="empleadoFilter">ID del empleado (opcional)</param>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 50, max: 1000)</param>
        /// <returns>Resultado paginado con análisis completo de asistencias</returns>
        [HttpGet("analisis/paginado")]
        public async Task<ActionResult<PagedResult<AsistenciaCompletaDto>>> GetAnalisisAsistenciasPaginadas(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string empleadoFilter = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validar parámetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 1000) pageSize = 1000;

                // Validar rango de fechas
                if (fechaFin < fechaInicio)
                {
                    return BadRequest(new
                    {
                        error = "Rango de fechas inválido",
                        message = "La fecha fin debe ser mayor o igual a la fecha inicio"
                    });
                }

                var resultado = await _iclockTransactionService.GetAnalisisAsistenciasCompletasPaginadasAsync(
                    fechaInicio, fechaFin, empleadoFilter, pageNumber, pageSize);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener análisis de asistencias paginadas",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ===== ENDPOINTS ADICIONALES DE UTILIDAD =====

        /// <summary>
        /// 🆕 Obtiene resumen estadístico de asistencias para un rango
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del rango</param>
        /// <param name="fechaFin">Fecha fin del rango</param>
        /// <param name="empleadoFilter">ID del empleado (opcional)</param>
        /// <returns>Resumen estadístico</returns>
        [HttpGet("resumen")]
        public async Task<ActionResult> GetResumenAsistencias(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string empleadoFilter = null)
        {
            try
            {
                var datos = await _iclockTransactionService.GetAnalisisAsistenciasCompletasAsync(
                    fechaInicio, fechaFin, empleadoFilter);

                var resumen = new
                {
                    totalDias = datos.Count,
                    diasPuntuales = datos.Count(d => d.EsPuntual),
                    diasConTardanza = datos.Count(d => d.TieneTardanza),
                    diasSinEntrada = datos.Count(d => !d.HoraEntrada.HasValue),
                    diasSinSalida = datos.Count(d => !d.HoraSalida.HasValue),
                    diasAsistenciaCompleta = datos.Count(d => d.AsistenciaCompleta),
                    promedioMinutosTardanza = datos.Where(d => d.MinutosTardanza.HasValue)
                                                   .Select(d => d.MinutosTardanza.Value)
                                                   .DefaultIfEmpty(0)
                                                   .Average(),
                    promedioHorasTrabajadas = datos.Where(d => d.HorasTrabajadas.HasValue)
                                                   .Select(d => (double)d.HorasTrabajadas.Value)
                                                   .DefaultIfEmpty(0)
                                                   .Average(),
                    porcentajePuntualidad = datos.Count > 0 ?
                        Math.Round((double)datos.Count(d => d.EsPuntual) / datos.Count * 100, 2) : 0
                };

                return Ok(new
                {
                    resumen,
                    periodo = new { fechaInicio, fechaFin },
                    empleado = empleadoFilter,
                    generadoEn = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al generar resumen de asistencias",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}