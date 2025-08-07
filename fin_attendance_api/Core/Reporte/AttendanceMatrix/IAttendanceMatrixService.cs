using Dtos.Reportes.Matrix;

namespace FibAttendanceApi.Core.Reporte.AttendanceMatrix
{
    /// <summary>
    /// Interfaz para el servicio de matriz de asistencia
    /// </summary>
    public interface IAttendanceMatrixService
    {
        Task<AttendanceMatrixResponseDto> GetAttendanceMatrixAsync(AttendanceMatrixFilterDto filter);
        Task<byte[]> ExportToExcelAsync(AttendanceMatrixFilterDto filter);
        Task<AttendanceMatrixPivotResponseDto> GetAttendanceMatrixPivotAsync(AttendanceMatrixFilterDto filter); // NUEVO

        Task<byte[]> ExportCostCenterReportAsync(AttendanceMatrixFilterDto filter);

        Task<byte[]> ExportMarkingsReportAsync(AttendanceMatrixFilterDto filter);

        Task<byte[]> ExportWeeklyAttendanceReportAsync(AttendanceMatrixFilterDto filter);


    }
}
