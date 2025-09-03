using Dtos.Reportes.HorasExtras;

namespace FibAttendanceApi.Core.Reporte.HorasExtras
{
    public interface IExtraHoursReportService
    {
        Task<ExtraHoursReportResult> GetExtraHoursReportDataAsync(ReportFiltersHE filters);
        Task<byte[]> ExportExtraHoursReportToExcelAsync(ReportFiltersHE filters);
    }

    public class ExtraHoursReportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ReporteAsistenciaSemanalDto> Data { get; set; } = new List<ReporteAsistenciaSemanalDto>();
    }
}