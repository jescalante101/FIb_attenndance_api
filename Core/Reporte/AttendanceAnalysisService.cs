using Dtos.Reportes.Simple.YourProject.DTOs;
using FibAttendanceApi.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Reporte
{

    public class AttendanceAnalysisService : IAttendanceAnalysisService
    {
        private readonly ApplicationDbcontext _context;

        public AttendanceAnalysisService(ApplicationDbcontext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AttendanceAnalysisResultDto>> GetAttendanceAnalysisAsync(
            AttendanceAnalysisRequestDto request)
        {
            var parameters = new[]
            {
                new SqlParameter("@FechaInicio", request.FechaInicio.Date),
                new SqlParameter("@FechaFin", request.FechaFin.Date),
                new SqlParameter("@EmployeeId", (object?)request.EmployeeId ?? DBNull.Value),
                new SqlParameter("@AreaId", (object?)request.AreaId ?? DBNull.Value),
                new SqlParameter("@LocationId", (object?)request.LocationId ?? DBNull.Value)
            };

            var sql = @"
                EXEC sp_AttendanceAnalysis 
                    @FechaInicio, 
                    @FechaFin, 
                    @EmployeeId, 
                    @AreaId, 
                    @LocationId";

            var results = new List<AttendanceAnalysisResultDto>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new AttendanceAnalysisResultDto
                        {
                            NroDoc = reader.IsDBNull("nro_doc") ? null : reader.GetString("nro_doc"),
                            EmployeeId = reader.IsDBNull("employee_id") ? null : reader.GetString("employee_id"),
                            FullNameEmployee = reader.IsDBNull("full_name_employee") ? null : reader.GetString("full_name_employee"),
                            AreaDescription = reader.IsDBNull("area_description") ? null : reader.GetString("area_description"),
                            LocationName = reader.IsDBNull("location_name") ? null : reader.GetString("location_name"),
                            AreaId = reader.IsDBNull("area_id") ? null : reader.GetString("area_id"),
                            LocationId = reader.IsDBNull("location_id") ? null : reader.GetString("location_id"),
                            Fecha = reader.GetDateTime("Fecha"),
                            ShiftName = reader.IsDBNull("ShiftName") ? null : reader.GetString("ShiftName"),
                            IntervalAlias = reader.IsDBNull("IntervalAlias") ? null : reader.GetString("IntervalAlias"),
                            TipoMarcacion = reader.IsDBNull("TipoMarcacion") ? null : reader.GetString("TipoMarcacion"),
                            TipoHorario = reader.IsDBNull("TipoHorario") ? null : reader.GetString("TipoHorario"),
                            ExceptionRemarks = reader.IsDBNull("ExceptionRemarks") ? null : reader.GetString("ExceptionRemarks"),
                            TipoPermiso = reader.IsDBNull("TipoPermiso") ? null : reader.GetString("TipoPermiso"),
                            DiasInfo = reader.IsDBNull("DiasInfo") ? null : reader.GetString("DiasInfo"),
                            HoraEsperada = reader.IsDBNull("HoraEsperada") ? null : reader.GetString("HoraEsperada"),
                            HoraMarcacionReal = reader.IsDBNull("HoraMarcacionReal") ? null : reader.GetString("HoraMarcacionReal"),
                            DiferenciaMinutos = reader.IsDBNull("DiferenciaMinutos") ? null : reader.GetInt32("DiferenciaMinutos"),
                            MinutosTardanza = reader.IsDBNull("MinutosTardanza") ? 0 : reader.GetInt32("MinutosTardanza"),
                            MinutosAdelanto = reader.IsDBNull("MinutosAdelanto") ? 0 : reader.GetInt32("MinutosAdelanto"),
                            EstadoMarcacion = reader.IsDBNull("EstadoMarcacion") ? null : reader.GetString("EstadoMarcacion"),
                            OrigenMarcacion = reader.IsDBNull("OrigenMarcacion") ? null : reader.GetString("OrigenMarcacion"),
                            InformacionAdicional = reader.IsDBNull("InformacionAdicional") ? null : reader.GetString("InformacionAdicional"),
                            RazonManual = reader.IsDBNull("RazonManual") ? null : reader.GetString("RazonManual"),
                            ValidacionRango = reader.IsDBNull("ValidacionRango") ? null : reader.GetString("ValidacionRango")
                        });
                    }
                }
            }

            return results;
        }

        public async Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(
            AttendanceAnalysisRequestDto request)
        {
            // Obtener datos detallados primero
            var detailedData = await GetAttendanceAnalysisAsync(request);

            // Agrupar y calcular resumen por empleado
            var summary = detailedData
                .Where(x => x.TipoMarcacion == "Entrada") // Solo contar entradas para evitar duplicados
                .GroupBy(x => new { x.EmployeeId, x.FullNameEmployee, x.AreaDescription, x.LocationName })
                .Select(g => new AttendanceSummaryDto
                {
                    EmployeeId = g.Key.EmployeeId,
                    FullNameEmployee = g.Key.FullNameEmployee,
                    AreaDescription = g.Key.AreaDescription,
                    LocationName = g.Key.LocationName,
                    TotalDias = g.Count(),
                    DiasPresente = g.Count(x => x.EstadoMarcacion == "PUNTUAL" || x.EstadoMarcacion == "TARDANZA"),
                    DiasFalta = g.Count(x => x.EstadoMarcacion == "FALTA"),
                    DiasVacaciones = g.Count(x => x.TipoPermiso == "VACACIONES"),
                    DiasPermiso = g.Count(x => x.TipoPermiso != null && x.TipoPermiso != "VACACIONES"),
                    TotalMinutosTardanza = g.Sum(x => x.MinutosTardanza),
                    DiasTardanza = g.Count(x => x.EstadoMarcacion == "TARDANZA"),
                    PromedioTardanza = g.Where(x => x.MinutosTardanza > 0).Any()
                        ? Math.Round(g.Where(x => x.MinutosTardanza > 0).Average(x => x.MinutosTardanza), 2)
                        : 0,
                    PorcentajeAsistencia = g.Count() > 0
                        ? Math.Round((double)g.Count(x => x.EstadoMarcacion == "PUNTUAL" || x.EstadoMarcacion == "TARDANZA") / g.Count() * 100, 2)
                        : 0
                })
                .OrderBy(x => x.FullNameEmployee)
                .ToList();

            return summary;
        }
    }
}

