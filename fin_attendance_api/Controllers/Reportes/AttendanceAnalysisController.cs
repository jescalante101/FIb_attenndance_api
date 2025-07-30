using Core.Reporte;
using Dtos.Reportes.Simple;
using Microsoft.AspNetCore.Mvc;

namespace FibAttendanceApi.Controllers.Reportes
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceAnalysisController : ControllerBase
    {
        private readonly IAttendanceAnalysisService _attendanceService;

        public AttendanceAnalysisController(IAttendanceAnalysisService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Obtiene análisis detallado de asistencia con filtros (sin paginación)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("detailed")]
        public async Task<ActionResult<IEnumerable<AttendanceAnalysisResultDto>>> GetDetailedAnalysis(
            [FromBody] AttendanceAnalysisRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.FechaInicio > request.FechaFin)
            {
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin");
            }

            var maxDays = 93; // Aproximadamente 3 meses
            if ((request.FechaFin - request.FechaInicio).TotalDays > maxDays)
            {
                return BadRequest($"El rango de fechas no puede exceder {maxDays} días");
            }

            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene análisis detallado de asistencia con paginación
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("detailed/paginated")]
        public async Task<ActionResult<PaginatedAttendanceResult>> GetDetailedAnalysisPaginated(
            [FromBody] PaginatedAttendanceAnalysisRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.FechaInicio > request.FechaFin)
            {
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin");
            }

            // Para paginación, permitimos rangos más grandes
            var maxDays = 365; // 1 año para paginación
            if ((request.FechaFin - request.FechaInicio).TotalDays > maxDays)
            {
                return BadRequest($"El rango de fechas no puede exceder {maxDays} días");
            }

            // Validaciones de paginación
            if (request.PageNumber < 1)
            {
                return BadRequest("El número de página debe ser mayor a 0");
            }

            if (request.PageSize < 1 || request.PageSize > 1000)
            {
                return BadRequest("El tamaño de página debe estar entre 1 y 1000");
            }

            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene análisis detallado paginado usando query parameters
        /// </summary>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="employeeId"></param>
        /// <param name="areaId"></param>
        /// <param name="locationId"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("detailed/paginated")]
        public async Task<ActionResult<PaginatedAttendanceResult>> GetDetailedAnalysisPaginatedQuery(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] string? employeeId = null,
            [FromQuery] string? areaId = null,
            [FromQuery] string? locationId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var request = new PaginatedAttendanceAnalysisRequestDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                EmployeeId = employeeId,
                AreaId = areaId,
                LocationId = locationId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetDetailedAnalysisPaginated(request);
        }

        /// <summary>
        /// Obtiene resumen de asistencia por empleado
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("summary")]
        public async Task<ActionResult<IEnumerable<AttendanceSummaryDto>>> GetSummaryAnalysis(
            [FromBody] AttendanceAnalysisRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var results = await _attendanceService.GetAttendanceSummaryAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene análisis de un empleado específico
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<PaginatedAttendanceResult>> GetEmployeeAnalysis(
            string employeeId,
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var request = new PaginatedAttendanceAnalysisRequestDto
            {
                EmployeeId = employeeId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene análisis por área
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("area/{areaId}")]
        public async Task<ActionResult<PaginatedAttendanceResult>> GetAreaAnalysis(
            string areaId,
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var request = new PaginatedAttendanceAnalysisRequestDto
            {
                AreaId = areaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene análisis por sede/location
        /// </summary>
        /// <param name="locationId"></param>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("location/{locationId}")]
        public async Task<ActionResult<PaginatedAttendanceResult>> GetLocationAnalysis(
            string locationId,
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var request = new PaginatedAttendanceAnalysisRequestDto
            {
                LocationId = locationId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(request);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Exporta datos completos a CSV (sin paginación para exportación completa)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromBody] AttendanceAnalysisRequestDto request)
        {
            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisAsync(request);

                var csv = GenerateCsv(results);
                var fileName = $"AsistenciaDetallada_{request.FechaInicio:yyyyMMdd}_{request.FechaFin:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar", details = ex.Message });
            }
        }

        /// <summary>
        /// Exporta datos paginados a CSV
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("export/csv-paginated")]
        public async Task<IActionResult> ExportToCsvPaginated([FromBody] PaginatedAttendanceAnalysisRequestDto request)
        {
            try
            {
                var results = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(request);

                var csv = GenerateCsv(results.Data);
                var fileName = $"AsistenciaDetallada_Pag{request.PageNumber}_{request.FechaInicio:yyyyMMdd}_{request.FechaFin:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar", details = ex.Message });
            }
        }

        /// <summary>
        /// Exporta resumen a CSV
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("export/summary-csv")]
        public async Task<IActionResult> ExportSummaryToCsv([FromBody] AttendanceAnalysisRequestDto request)
        {
            try
            {
                var results = await _attendanceService.GetAttendanceSummaryAsync(request);

                var csv = GenerateSummaryCsv(results);
                var fileName = $"ResumenAsistencia_{request.FechaInicio:yyyyMMdd}_{request.FechaFin:yyyyMMdd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al exportar", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene estadísticas rápidas de paginación sin datos
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("stats")]
        public async Task<ActionResult<object>> GetPaginationStats([FromBody] AttendanceAnalysisRequestDto request)
        {
            try
            {
                // Usamos paginación con pageSize = 1 solo para obtener el total
                var paginatedRequest = new PaginatedAttendanceAnalysisRequestDto
                {
                    FechaInicio = request.FechaInicio,
                    FechaFin = request.FechaFin,
                    EmployeeId = request.EmployeeId,
                    AreaId = request.AreaId,
                    LocationId = request.LocationId,
                    PageNumber = 1,
                    PageSize = 1
                };

                var result = await _attendanceService.GetAttendanceAnalysisPaginatedAsync(paginatedRequest);

                return Ok(new
                {
                    TotalRecords = result.TotalRecords,
                    RecommendedPageSize = Math.Min(100, Math.Max(10, result.TotalRecords / 10)),
                    EstimatedPages50 = (int)Math.Ceiling((double)result.TotalRecords / 50),
                    EstimatedPages100 = (int)Math.Ceiling((double)result.TotalRecords / 100)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener estadísticas", details = ex.Message });
            }
        }

        private string GenerateCsv(IEnumerable<AttendanceAnalysisResultDto> data)
        {
            var csv = new System.Text.StringBuilder();

            // Headers
            csv.AppendLine("NroDoc,EmployeeId,FullNameEmployee,AreaDescription,LocationName,Fecha,ShiftName,TipoMarcacion,HoraEsperada,HoraMarcacionReal,DiferenciaMinutos,EstadoMarcacion,TipoPermiso,OrigenMarcacion");

            // Data
            foreach (var item in data)
            {
                csv.AppendLine($"{item.NroDoc},{item.EmployeeId},{item.FullNameEmployee},{item.AreaDescription},{item.LocationName},{item.Fecha:yyyy-MM-dd},{item.ShiftName},{item.TipoMarcacion},{item.HoraEsperada},{item.HoraMarcacionReal},{item.DiferenciaMinutos},{item.EstadoMarcacion},{item.TipoPermiso},{item.OrigenMarcacion}");
            }

            return csv.ToString();
        }

        private string GenerateSummaryCsv(IEnumerable<AttendanceSummaryDto> data)
        {
            var csv = new System.Text.StringBuilder();

            // Headers
            csv.AppendLine("EmployeeId,FullNameEmployee,AreaDescription,LocationName,TotalDias,DiasPresente,DiasFalta,DiasVacaciones,TotalMinutosTardanza,DiasTardanza,PromedioTardanza,PorcentajeAsistencia");

            // Data
            foreach (var item in data)
            {
                csv.AppendLine($"{item.EmployeeId},{item.FullNameEmployee},{item.AreaDescription},{item.LocationName},{item.TotalDias},{item.DiasPresente},{item.DiasFalta},{item.DiasVacaciones},{item.TotalMinutosTardanza},{item.DiasTardanza},{item.PromedioTardanza},{item.PorcentajeAsistencia}");
            }

            return csv.ToString();
        }
    }
}