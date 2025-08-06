using Dtos.Reportes.Matrix;
using FibAttendanceApi.Core.Reporte.AttendanceMatrix;
using Microsoft.AspNetCore.Mvc;

namespace FibAttendanceApi.Controllers.Reportes
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceMatrixController : ControllerBase
    {
        private readonly IAttendanceMatrixService _attendanceMatrixService;

        public AttendanceMatrixController(IAttendanceMatrixService attendanceMatrixService)
        {
            _attendanceMatrixService = attendanceMatrixService;
        }

        [HttpPost("matrix")]
        public async Task<ActionResult<AttendanceMatrixResponseDto>> GetMatrix([FromBody] AttendanceMatrixFilterDto filter)
        {

            

            var result = await _attendanceMatrixService.GetAttendanceMatrixAsync(filter);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }


        [HttpPost("export")]
        public async Task<IActionResult> ExportToExcel([FromBody] AttendanceMatrixFilterDto filter)
        {
            try
            {
                // Validaciones
                if (filter.FechaFin < filter.FechaInicio)
                {
                    return BadRequest(new { message = "La fecha fin debe ser mayor o igual a la fecha inicio" });
                }

                var daysDifference = (filter.FechaFin - filter.FechaInicio).Days;
                if (daysDifference > 31)
                {
                    return BadRequest(new { message = "El rango de fechas no puede ser mayor a 31 días" });
                }

                //_logger.LogInformation("Iniciando exportación de matriz de asistencia para período {FechaInicio} - {FechaFin}",
                    //filter.FechaInicio, filter.FechaFin);

                var excelData = await _attendanceMatrixService.ExportToExcelAsync(filter);

                var fileName = $"Matriz_Asistencia_{filter.FechaInicio:yyyyMMdd}_{filter.FechaFin:yyyyMMdd}_{DateTime.Now:HHmm}.xlsx";

                //_logger.LogInformation("Exportación completada exitosamente. Archivo: {FileName}, Tamaño: {FileSize} bytes",
                   // fileName, excelData.Length);

                return File(excelData,
                           "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           fileName);
            }
            catch (InvalidOperationException ex)
            {
                //_logger.LogWarning("No hay datos para exportar: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error al exportar matriz de asistencia");
                return StatusCode(500, new { message = "Error interno del servidor al generar el reporte" });
            }
        }

        // En el controller, agregar:
        [HttpPost("pivot")]
        public async Task<ActionResult<AttendanceMatrixPivotResponseDto>> GetMatrixPivot([FromBody] AttendanceMatrixFilterDto filter)
        {
            var result = await _attendanceMatrixService.GetAttendanceMatrixPivotAsync(filter);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

    }
}
