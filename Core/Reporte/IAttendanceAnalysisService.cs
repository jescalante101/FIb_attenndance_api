using Dtos.Reportes.Simple.YourProject.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Reporte
{
    public interface IAttendanceAnalysisService
    {
        Task<IEnumerable<AttendanceAnalysisResultDto>> GetAttendanceAnalysisAsync(
            AttendanceAnalysisRequestDto request);

        Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(
            AttendanceAnalysisRequestDto request);
    }
}
