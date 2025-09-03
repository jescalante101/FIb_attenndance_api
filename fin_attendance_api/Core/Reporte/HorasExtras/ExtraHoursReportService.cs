using ClosedXML.Excel;
using Dapper;
using Dtos.Reportes.HorasExtras;
using FibAttendanceApi.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace FibAttendanceApi.Core.Reporte.HorasExtras
{
    public class ExtraHoursReportService : IExtraHoursReportService
    {
        private readonly ApplicationDbcontext _context;

        public ExtraHoursReportService(ApplicationDbcontext context)
        {
            _context = context;
        }

        public async Task<ExtraHoursReportResult> GetExtraHoursReportDataAsync(ReportFiltersHE filters)
        {
            try
            {
                var datosPlanos = await GetRawDataFromStoredProcedure(filters);
                var datosPivoteados = ProcessDataToWeeklyReport(datosPlanos, filters.StartDate, filters.EndDate);

                return new ExtraHoursReportResult
                {
                    Success = true,
                    Data = datosPivoteados,
                    Message = "Datos obtenidos correctamente"
                };
            }
            catch (Exception ex)
            {
                return new ExtraHoursReportResult
                {
                    Success = false,
                    Data = new List<ReporteAsistenciaSemanalDto>(),
                    Message = $"Error al obtener los datos: {ex.Message}"
                };
            }
        }

        public async Task<byte[]> ExportExtraHoursReportToExcelAsync(ReportFiltersHE filters)
        {
            var datosPlanos = await GetRawDataFromStoredProcedure(filters);
            var datosPivoteados = ProcessDataToWeeklyReport(datosPlanos, filters.StartDate, filters.EndDate);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte Horas Extras");
                
                CreateExcelReport(worksheet, datosPivoteados, filters);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private async Task<List<AsistenciaDiariaSpDto>> GetRawDataFromStoredProcedure(ReportFiltersHE filters)
        {
            var connectionString = _context.Database.GetConnectionString();
            
            using var connection = new SqlConnection(connectionString);
            
            var parameters = new DynamicParameters();
            parameters.Add("@FechaInicio", filters.StartDate, DbType.DateTime);
            parameters.Add("@FechaFin", filters.EndDate, DbType.DateTime);
            parameters.Add("@CompaniaId", filters.CompanyId, DbType.String);
            parameters.Add("@EmployeeIds", string.IsNullOrEmpty(filters.EmployeeIds) ? null : filters.EmployeeIds, DbType.String);
            parameters.Add("@AreaId", string.IsNullOrEmpty(filters.AreaId) ? null : filters.AreaId, DbType.String);
            parameters.Add("@SedeId", string.IsNullOrEmpty(filters.SedeId) ? null : filters.SedeId, DbType.String);

            var result = await connection.QueryAsync<AsistenciaDiariaSpDto>(
                "sp_AttendanceMatrixCalculated",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        private List<ReporteAsistenciaSemanalDto> ProcessDataToWeeklyReport(List<AsistenciaDiariaSpDto> datosPlanos, DateTime startDate, DateTime endDate)
        {
            var weeks = GetWeeksInRange(startDate, endDate);
            
            var result = datosPlanos
                .GroupBy(d => new { d.Nro_Doc, d.Colaborador })
                .Select(employeeGroup => {
                    var firstRecord = employeeGroup.First();
                    return new ReporteAsistenciaSemanalDto
                    {
                        Nro_Doc = employeeGroup.Key.Nro_Doc,
                        Colaborador = employeeGroup.Key.Colaborador,
                        Area = firstRecord.Area,
                        Sede = firstRecord.Sede,
                        Cargo = firstRecord.Cargo,
                        FechaIngreso = firstRecord.Fecha,
                        AsistenciaPorDia = ProcessEmployeeWeeklyData(employeeGroup.ToList(), weeks)
                    };
                })
                .ToList();

            // Calcular totales para cada empleado
            foreach (var empleado in result)
            {
                empleado.TotalHorasNormales = empleado.AsistenciaPorDia.Values.Sum(v => v.HorasNormales);
                empleado.TotalHorasExtras1 = empleado.AsistenciaPorDia.Values.Sum(v => v.HorasExtras1);
                empleado.TotalHorasExtras2 = empleado.AsistenciaPorDia.Values.Sum(v => v.HorasExtras2);
                empleado.TotalHorasExtras100 = empleado.AsistenciaPorDia.Values.Sum(v => v.HorasExtras100);
                empleado.TotalVacaciones = empleado.AsistenciaPorDia.Values.Count(v => v.Estado == "VACACIONES");
                empleado.TotalFaltas = empleado.AsistenciaPorDia.Values.Count(v => v.Estado == "FALTA");
                empleado.TotalPermisos = empleado.AsistenciaPorDia.Values.Count(v => v.Estado == "PERMISO");
            }

            return result;
        }

        private Dictionary<DateTime, AsistenciaDiaReporteDto> ProcessEmployeeWeeklyData(List<AsistenciaDiariaSpDto> empleadoDatos, List<(DateTime start, DateTime end)> weeks)
        {
            var result = new Dictionary<DateTime, AsistenciaDiaReporteDto>();

            foreach (var dia in empleadoDatos)
            {
                var asistenciaDia = new AsistenciaDiaReporteDto
                {
                    HoraEntrada = string.IsNullOrEmpty(dia.HoraEntrada) ? "FALTA" : dia.HoraEntrada,
                    HoraSalida = string.IsNullOrEmpty(dia.HoraSalida) ? "FALTA" : dia.HoraSalida,
                    HorasNormales = Math.Round(dia.HorasNormales / 60.0, 2),
                    HorasExtras1 = Math.Round(dia.HorasExtrasNivel1 / 60.0, 2),
                    HorasExtras2 = Math.Round(dia.HorasExtrasNivel2 / 60.0, 2),
                    HorasExtras100 = Math.Round(dia.HorasExtras100 / 60.0, 2),
                    Estado = DetermineAttendanceStatus(dia),
                    TipoTurno = dia.TipoTurno
                };

                result[dia.Fecha.Date] = asistenciaDia;
            }

            return result;
        }

        private string DetermineAttendanceStatus(AsistenciaDiariaSpDto dia)
        {
            if (string.IsNullOrEmpty(dia.HoraEntrada) || dia.HoraEntrada.Contains("FALTA") || dia.HoraEntrada.Contains("Falta"))
                return "FALTA";
            if (dia.HoraEntrada.Contains("VACACION") || dia.HoraSalida.Contains("VACACION"))
                return "VACACIONES";
            if (dia.HoraEntrada.Contains("PERMISO") || dia.HoraSalida.Contains("PERMISO") ||
                dia.HoraEntrada.Contains("S.I.") || dia.HoraSalida.Contains("S.I.") || 
                dia.HoraEntrada.Contains("ENFERMEDAD") || dia.HoraSalida.Contains("ENFERMEDAD") ||
                dia.HoraEntrada.Contains("ACCIDENTE") || dia.HoraSalida.Contains("ACCIDENTE") ||
                dia.HoraEntrada.Contains("SUBSIDIO") || dia.HoraSalida.Contains("SUBSIDIO"))
                return "PERMISO";
            return "PRESENTE";
        }

        private List<(DateTime start, DateTime end)> GetWeeksInRange(DateTime startDate, DateTime endDate)
        {
            var weeks = new List<(DateTime start, DateTime end)>();
            var current = startDate;

            while (current <= endDate)
            {
                // Buscar el próximo domingo o el final del rango
                var weekEnd = current;
                while (weekEnd.DayOfWeek != DayOfWeek.Sunday && weekEnd < endDate)
                {
                    weekEnd = weekEnd.AddDays(1);
                }
                
                // Si no encontramos domingo, usar el final del rango
                if (weekEnd > endDate)
                    weekEnd = endDate;

                weeks.Add((current, weekEnd));
                current = weekEnd.AddDays(1);
            }

            return weeks;
        }

        private DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private void CreateExcelReport(IXLWorksheet worksheet, List<ReporteAsistenciaSemanalDto> data, ReportFiltersHE filters)
        {
            // Título principal
            var titulo = $"REPORTE DE ASISTENCIA SEMANAL - {filters.StartDate:yyyy-MM-dd}-{filters.EndDate:yyyy-MM-dd}";
            worksheet.Cell("A1").Value = titulo;
            worksheet.Range("A1:Z1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Cabeceras principales
            int currentRow = 3;
            int currentCol = 1;

            // Columnas fijas
            worksheet.Cell(currentRow, currentCol++).Value = "ITEM";
            worksheet.Cell(currentRow, currentCol++).Value = "NRO DOC";
            worksheet.Cell(currentRow, currentCol++).Value = "APELLIDOS Y NOMBRES";
            worksheet.Cell(currentRow, currentCol++).Value = "AREA";
            worksheet.Cell(currentRow, currentCol++).Value = "SEDE";
            worksheet.Cell(currentRow, currentCol++).Value = "CARGO";
            worksheet.Cell(currentRow, currentCol++).Value = "FECHA INGRESO";

            // Obtener semanas para crear cabeceras dinámicas
            var weeks = GetWeeksInRange(filters.StartDate, filters.EndDate);
            
            foreach (var (weekStart, weekEnd) in weeks)
            {
                var weekNumber = weeks.FindIndex(w => w.start == weekStart && w.end == weekEnd) + 1;
                var weekTitle = $"SEMANA {weekNumber}";
                var startCol = currentCol;

                // TURNO - Primera columna de la semana
                worksheet.Cell(currentRow, currentCol).Value = "TURNO";
                worksheet.Cell(currentRow + 1, currentCol).Value = "SEMANAL";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";
                
                // Crear cabeceras para cada día de la semana con fechas específicas
                var daysInWeek = GetDaysInWeek(weekStart, weekEnd);
                foreach (var day in daysInWeek)
                {
                    // Fila 1: Día de la semana (merge 6 columnas - sin turno)
                    var dayName = day.ToString("ddd", new CultureInfo("es-ES")).ToUpper();
                    worksheet.Cell(currentRow, currentCol).Value = dayName;
                    worksheet.Range(currentRow, currentCol, currentRow, currentCol + 5).Merge();
                    worksheet.Cell(currentRow, currentCol).Style
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Font.SetBold(true);
                    
                    // Fila 2: Fecha corta (merge 6 columnas)
                    worksheet.Cell(currentRow + 1, currentCol).Value = day.ToString("dd/MM");
                    worksheet.Range(currentRow + 1, currentCol, currentRow + 1, currentCol + 5).Merge();
                    worksheet.Cell(currentRow + 1, currentCol).Style
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Font.SetBold(true);
                    
                    // Fila 3: Subcabeceras compactas
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "ENT";
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "SAL";
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "H.N";
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "H.E 25%";
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "H.E 35%";
                    worksheet.Cell(currentRow + 2, currentCol++).Value = "H.E 100%";
                }
                
                // Columnas de totales al final de cada semana - COMPACTAS
                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "H.N";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";
                
                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "H.E 25%";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "H.E 35%";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "H.E 100%";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "VAC";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "FAL";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                worksheet.Cell(currentRow, currentCol).Value = "TOT";
                worksheet.Cell(currentRow + 1, currentCol).Value = "PER";
                worksheet.Cell(currentRow + 2, currentCol++).Value = "";

                // Ya agregamos las columnas de totales arriba

                // Merge para el título de la semana
                var endCol = currentCol - 1;
                if (endCol > startCol)
                {
                    worksheet.Range(currentRow, startCol, currentRow, endCol).Merge();
                    worksheet.Cell(currentRow, startCol).Value = weekTitle;
                    worksheet.Cell(currentRow, startCol).Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.LightBlue)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }
            }

            // Aplicar estilos FIORI a las cabeceras principales (ahora son 3 filas)
            var headerRange = worksheet.Range(currentRow, 1, currentRow + 2, currentCol - 1);
            headerRange.Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#354A5F")) // headerBackground
                .Font.SetFontColor(XLColor.FromHtml("#FFFFFF")) // headerText
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // Estilo especial para cabeceras de columnas fijas
            var fixedColumnsRange = worksheet.Range(currentRow, 1, currentRow + 2, 7);
            fixedColumnsRange.Style
                .Fill.SetBackgroundColor(XLColor.FromHtml("#354A5F"))
                .Font.SetFontColor(XLColor.FromHtml("#FFFFFF"));

            // Datos de empleados (ahora empezamos 3 filas más abajo)
            currentRow += 3;
            int itemNumber = 1;

            foreach (var empleado in data)
            {
                currentCol = 1;
                worksheet.Cell(currentRow, currentCol++).Value = itemNumber++;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.Nro_Doc;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.Colaborador;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.Area;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.Sede;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.Cargo;
                worksheet.Cell(currentRow, currentCol++).Value = empleado.FechaIngreso?.ToString("dd/MM/yyyy") ?? "";

                // Datos por semana
                foreach (var (weekStart, weekEnd) in weeks)
                {
                    var daysInWeek = GetDaysInWeek(weekStart, weekEnd);
                    
                    // TURNO SEMANAL - Primera columna de la semana
                    var turnoSemanal = "N/A";
                    foreach (var day in daysInWeek)
                    {
                        if (empleado.AsistenciaPorDia.TryGetValue(day.Date, out var asist) && !string.IsNullOrEmpty(asist.TipoTurno))
                        {
                            turnoSemanal = asist.TipoTurno;
                            break;
                        }
                    }
                    worksheet.Cell(currentRow, currentCol++).Value = turnoSemanal;
                    
                    foreach (var day in daysInWeek)
                    {
                        if (empleado.AsistenciaPorDia.TryGetValue(day.Date, out var asistencia))
                        {
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HoraEntrada;
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HoraSalida;
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HorasNormales;
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HorasExtras1;
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HorasExtras2;
                            worksheet.Cell(currentRow, currentCol++).Value = asistencia.HorasExtras100;

                            // Aplicar colores FIORI según estado para ENTRADA y SALIDA
                            var entradaSalidaRange = worksheet.Range(currentRow, currentCol - 6, currentRow, currentCol - 5);
                            if (asistencia.Estado == "FALTA")
                            {
                                entradaSalidaRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#ED6A5A")); // negative.background
                                entradaSalidaRange.Style.Font.SetFontColor(XLColor.FromHtml("#FFFFFF")); // negative.text
                            }
                            else if (asistencia.Estado == "VACACIONES")
                            {
                                entradaSalidaRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EBF8FF")); // vacacionesBackground
                            }
                            else if (asistencia.Estado == "PERMISO")
                            {
                                entradaSalidaRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5")); // permisoBackground
                            }
                            else
                            {
                                entradaSalidaRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F2F2F2")); // weekendBackground
                            }

                            // Colores FIORI para las columnas de horas - GRADIENTE DE INTENSIDAD
                            worksheet.Cell(currentRow, currentCol - 4).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAECEE")); // H.N - totalsBackground
                            worksheet.Cell(currentRow, currentCol - 3).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#B5E1E0")); // 25% - positiveGradient level 1
                            worksheet.Cell(currentRow, currentCol - 2).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#75C4C2")); // 35% - positiveGradient level 2
                            worksheet.Cell(currentRow, currentCol - 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#36A9A3")); // 100% - positiveGradient level 3
                            worksheet.Cell(currentRow, currentCol - 1).Style.Font.SetFontColor(XLColor.FromHtml("#FFFFFF")); // text para 100%
                        }
                        else
                        {
                            // Día sin datos (todas las 6 columnas - sin turno)
                            worksheet.Cell(currentRow, currentCol++).Value = "";
                            worksheet.Cell(currentRow, currentCol++).Value = "";
                            worksheet.Cell(currentRow, currentCol++).Value = 0;
                            worksheet.Cell(currentRow, currentCol++).Value = 0;
                            worksheet.Cell(currentRow, currentCol++).Value = 0;
                            worksheet.Cell(currentRow, currentCol++).Value = 0;
                        }
                    }

                    // Totales de la semana - DETALLADOS
                    var weeklyTotals = CalculateWeeklyTotals(empleado, daysInWeek);
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.horasNormales;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.horasExtras1;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.horasExtras2;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.horasExtras100;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.vacaciones;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.faltas;
                    worksheet.Cell(currentRow, currentCol++).Value = weeklyTotals.permisos;
                    
                    // Aplicar colores FIORI a las columnas de totales
                    worksheet.Cell(currentRow, currentCol - 7).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAECEE")); // H.N - totalsBackground
                    worksheet.Cell(currentRow, currentCol - 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#B5E1E0")); // H.E 25% - positiveGradient level 1
                    worksheet.Cell(currentRow, currentCol - 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#75C4C2")); // H.E 35% - positiveGradient level 2
                    worksheet.Cell(currentRow, currentCol - 4).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#36A9A3")); // H.E 100% - positiveGradient level 3
                    worksheet.Cell(currentRow, currentCol - 4).Style.Font.SetFontColor(XLColor.FromHtml("#FFFFFF")); // text para 100%
                    worksheet.Cell(currentRow, currentCol - 3).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EBF8FF")); // Vacaciones - informative
                    worksheet.Cell(currentRow, currentCol - 2).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#ED6A5A")); // Faltas - negative
                    worksheet.Cell(currentRow, currentCol - 2).Style.Font.SetFontColor(XLColor.FromHtml("#FFFFFF"));
                    worksheet.Cell(currentRow, currentCol - 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5")); // Permisos - neutral
                }

                currentRow++;
            }

            // AGREGAR FILA DE TOTALES GENERALES AL FINAL
            currentRow += 2; // Dejar espacio

            // Título de totales
            worksheet.Cell(currentRow, 1).Value = "TOTALES GENERALES";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#37474F"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            currentRow++;

            // Calcular totales generales
            var totalGeneralHorasNormales = data.Sum(e => e.TotalHorasNormales);
            var totalGeneralHorasExtras1 = data.Sum(e => e.TotalHorasExtras1);
            var totalGeneralHorasExtras2 = data.Sum(e => e.TotalHorasExtras2);
            var totalGeneralHorasExtras100 = data.Sum(e => e.TotalHorasExtras100);
            var totalGeneralVacaciones = data.Sum(e => e.TotalVacaciones);
            var totalGeneralFaltas = data.Sum(e => e.TotalFaltas);
            var totalGeneralPermisos = data.Sum(e => e.TotalPermisos);

            // Escribir totales generales
            currentCol = 1;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL H.TRAB:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralHorasNormales;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL H.E 25%:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralHorasExtras1;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL H.E 35%:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralHorasExtras2;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL H.E 100%:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralHorasExtras100;

            currentRow++;
            currentCol = 1;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL VACACIONES:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralVacaciones;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL FALTAS:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralFaltas;
            worksheet.Cell(currentRow, currentCol++).Value = "TOTAL PERMISOS:";
            worksheet.Cell(currentRow, currentCol++).Value = totalGeneralPermisos;

            // Aplicar estilos a las filas de totales
            var totalesRange = worksheet.Range(currentRow - 1, 1, currentRow, currentCol - 1);
            totalesRange.Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#ECEFF1"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Aplicar bordes a toda la tabla 
            var dataRange = worksheet.Range(3, 1, currentRow, currentCol - 1);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Ajustar ancho de columnas específicamente
            worksheet.Column(1).Width = 6;  // ITEM
            worksheet.Column(2).Width = 12; // NRO DOC
            worksheet.Column(3).Width = 25; // NOMBRES
            worksheet.Column(4).Width = 15; // AREA
            worksheet.Column(5).Width = 15; // SEDE
            worksheet.Column(6).Width = 18; // CARGO
            worksheet.Column(7).Width = 12; // FECHA INGRESO

            // Ajustar columnas de datos de días - más compactas
            var currentColNum = 8;
            foreach (var (weekStart, weekEnd) in weeks)
            {
                // TURNO columna
                worksheet.Column(currentColNum++).Width = 8;
                
                var daysInWeek = GetDaysInWeek(weekStart, weekEnd);
                foreach (var day in daysInWeek)
                {
                    worksheet.Column(currentColNum++).Width = 6;  // ENT - más pequeño
                    worksheet.Column(currentColNum++).Width = 6;  // SAL - más pequeño  
                    worksheet.Column(currentColNum++).Width = 6;  // H.N
                    worksheet.Column(currentColNum++).Width = 8;  // H.E 25%
                    worksheet.Column(currentColNum++).Width = 8;  // H.E 35%
                    worksheet.Column(currentColNum++).Width = 8;  // H.E 100%
                }
                
                // Columnas de totales semanales (ahora son 7 en vez de 8)
                for (int i = 0; i < 7; i++)
                {
                    worksheet.Column(currentColNum++).Width = 6;
                }
            }
        }

        private List<DateTime> GetDaysInWeek(DateTime weekStart, DateTime weekEnd)
        {
            var days = new List<DateTime>();
            for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
            {
                days.Add(date);
            }
            return days;
        }

        private (double horasNormales, double horasExtras1, double horasExtras2, double horasExtras100, int vacaciones, int faltas, int permisos) CalculateWeeklyTotals(
            ReporteAsistenciaSemanalDto empleado, List<DateTime> weekDays)
        {
            double horasNormales = 0;
            double horasExtras1 = 0;
            double horasExtras2 = 0;
            double horasExtras100 = 0;
            int vacaciones = 0;
            int faltas = 0;
            int permisos = 0;

            foreach (var day in weekDays)
            {
                if (empleado.AsistenciaPorDia.TryGetValue(day.Date, out var asistencia))
                {
                    horasNormales += asistencia.HorasNormales;
                    horasExtras1 += asistencia.HorasExtras1;
                    horasExtras2 += asistencia.HorasExtras2;
                    horasExtras100 += asistencia.HorasExtras100;

                    if (asistencia.Estado == "VACACIONES") vacaciones++;
                    else if (asistencia.Estado == "FALTA") faltas++;
                    else if (asistencia.Estado == "PERMISO") permisos++;
                }
            }

            return (Math.Round(horasNormales, 2), Math.Round(horasExtras1, 2), Math.Round(horasExtras2, 2), Math.Round(horasExtras100, 2), vacaciones, faltas, permisos);
        }
    }
}