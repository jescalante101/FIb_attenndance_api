using Dtos.Reportes.HorasExtras;
using FibAttendanceApi.Core.Reporte.HorasExtras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FibAttendanceApi.Controllers.Reportes
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExtraHoursReportController : ControllerBase
    {
        private readonly IExtraHoursReportService _extraHoursReportService;

        public ExtraHoursReportController(IExtraHoursReportService extraHoursReportService)
        {
            _extraHoursReportService = extraHoursReportService;
        }

        [HttpPost("data")]
        public async Task<ActionResult> GetExtraHoursReportData([FromBody] ReportFiltersHE filters)
        {
            try
            {
                // Validaciones básicas
                if (filters.EndDate < filters.StartDate)
                {
                    return BadRequest(new { message = "La fecha fin debe ser mayor o igual a la fecha inicio" });
                }

                var daysDifference = (filters.EndDate - filters.StartDate).Days;
                if (daysDifference > 30) // Máximo 2 meses
                {
                    return BadRequest(new { message = "El rango de fechas no puede ser mayor a 62 días" });
                }

                if (string.IsNullOrEmpty(filters.CompanyId))
                {
                    return BadRequest(new { message = "El ID de compañía es obligatorio" });
                }

                var result = await _extraHoursReportService.GetExtraHoursReportDataAsync(filters);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor al obtener datos del reporte de horas extras", details = ex.Message });
            }
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportExtraHoursReport([FromBody] ReportFiltersHE filters)
        {
            try
            {
                // Validaciones básicas
                if (filters.EndDate < filters.StartDate)
                {
                    return BadRequest(new { message = "La fecha fin debe ser mayor o igual a la fecha inicio" });
                }

                var daysDifference = (filters.EndDate - filters.StartDate).Days;
                if (daysDifference > 62) // Máximo 2 meses
                {
                    return BadRequest(new { message = "El rango de fechas no puede ser mayor a 62 días" });
                }

                if (string.IsNullOrEmpty(filters.CompanyId))
                {
                    return BadRequest(new { message = "El ID de compañía es obligatorio" });
                }

                var excelData = await _extraHoursReportService.ExportExtraHoursReportToExcelAsync(filters);

                var fileName = $"Reporte_Horas_Extras_{filters.StartDate:yyyyMMdd}_{filters.EndDate:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";

                return File(excelData,
                           "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor al generar el reporte de horas extras", details = ex.Message });
            }
        }

        [HttpPost("summary")]
        public async Task<ActionResult> GetReportSummary([FromBody] ReportFiltersHE filters)
        {
            try
            {
                var result = await _extraHoursReportService.GetExtraHoursReportDataAsync(filters);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                var summary = new
                {
                    TotalEmpleados = result.Data.Count,
                    TotalHorasNormales = result.Data.Sum(e => e.TotalHorasNormales),
                    TotalHorasExtras25 = result.Data.Sum(e => e.TotalHorasExtras1),
                    TotalHorasExtras35 = result.Data.Sum(e => e.TotalHorasExtras2),
                    TotalHorasExtras100 = result.Data.Sum(e => e.TotalHorasExtras100),
                    FechaGeneracion = DateTime.Now,
                    PeriodoReporte = new
                    {
                        FechaInicio = filters.StartDate,
                        FechaFin = filters.EndDate,
                        TotalDias = (filters.EndDate - filters.StartDate).Days + 1
                    }
                };

                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al generar resumen del reporte", details = ex.Message });
            }
        }

      
    }
}