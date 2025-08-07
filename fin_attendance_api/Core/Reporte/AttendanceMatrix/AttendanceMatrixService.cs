using ClosedXML.Excel;
using Dtos.Reportes.Matrix;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Drawing;
using System.Globalization;

namespace FibAttendanceApi.Core.Reporte.AttendanceMatrix
{
    /// <summary>
    /// Servicio para manejo de matriz de asistencia
    /// </summary>
    public class AttendanceMatrixService : IAttendanceMatrixService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceMatrixService> _logger;
        private readonly string _connectionString;

        public AttendanceMatrixService(
            IConfiguration configuration,
            ILogger<AttendanceMatrixService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Obtiene datos de matriz de asistencia
        /// </summary>
        public async Task<AttendanceMatrixResponseDto> GetAttendanceMatrixAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            var response = new AttendanceMatrixResponseDto
            {
                Data = new List<AttendanceMatrixDto>(),
                GeneratedAt = startTime
            };

            try
            {
                _logger.LogInformation("Iniciando consulta de matriz de asistencia para período {FechaInicio} - {FechaFin}",
                    filter.FechaInicio, filter.FechaFin);

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("sp_AttendanceMatrixOptimized", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300 // 5 minutos timeout
                };

                // Agregar parámetros
                AddParameters(command, filter);

                await connection.OpenAsync();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToDto(reader));
                }

                response.TotalRecords = response.Data[0].TotalRecords ?? 0;
                response.CurrentPage = response.Data[0].CurrentPage ?? 0;
                response.PageSize = response.Data[0].PageSize ?? 0;
                response.TotalPages = response.Data[0].TotalPages ?? 0.0;
                response.Success = true;
                response.Message = $"Consulta exitosa. {response.TotalRecords} registros encontrados.";

                _logger.LogInformation("Consulta completada exitosamente. {TotalRecords} registros obtenidos",
                    response.TotalRecords);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al ejecutar consulta de matriz de asistencia");
                response.Success = false;
                response.Message = $"Error en base de datos: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener matriz de asistencia");
                response.Success = false;
                response.Message = $"Error interno: {ex.Message}";
            }

            response.ExecutionTime = DateTime.Now - startTime;
            return response;
        }

        // <summary>
        /// Exporta datos a Excel con formato matriz pivoteada
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("Iniciando exportación a Excel para período {FechaInicio} - {FechaFin}",
                filter.FechaInicio, filter.FechaFin);

            try
            {
                // 1. Obtener datos del SP
                var data = await GetAttendanceNoPaginatedAsync(filter);

                if (!data.Success || !data.Data.Any())
                {
                    throw new InvalidOperationException("No hay datos para exportar");
                }

                // 2. Procesar datos y hacer pivot
                var pivotData = ProcessDataForPivot(data.Data, filter.FechaInicio, filter.FechaFin);

                // 3. Generar Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Matriz de Asistencia");

                // 4. Configurar formato y generar contenido
                GenerateExcelContent(worksheet, pivotData, filter.FechaInicio, filter.FechaFin);

                // 5. Convertir a bytes
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var result = stream.ToArray();

                _logger.LogInformation("Exportación completada. {TotalEmployees} empleados, {FileSize} bytes",
                    pivotData.Count, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar matriz de asistencia a Excel");
                throw;
            }
        }

        /// <summary>
        /// Exporta reporte de marcaciones mostrando cantidad de marcaciones por día
        /// </summary>
        public async Task<byte[]> ExportMarkingsReportAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("Iniciando exportación de Reporte de Marcaciones para período {FechaInicio} - {FechaFin}",
                filter.FechaInicio, filter.FechaFin);

            try
            {
                // 1. Obtener datos del SP
                var data = await GetAttendanceNoPaginatedAsync(filter);

                if (!data.Success || !data.Data.Any())
                {
                    throw new InvalidOperationException("No hay datos para exportar");
                }

                // 2. Procesar datos y hacer pivot
                var pivotData = ProcessDataForPivot(data.Data, filter.FechaInicio, filter.FechaFin);

                // 3. Generar Excel con reporte de Marcaciones
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Reporte Marcaciones");

                // 4. Configurar formato y generar contenido
                GenerateMarkingsReport(worksheet, pivotData, filter.FechaInicio, filter.FechaFin);

                // 5. Convertir a bytes
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var result = stream.ToArray();

                _logger.LogInformation("Exportación de Marcaciones completada. {TotalEmployees} empleados, {FileSize} bytes",
                    pivotData.Count, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de marcaciones a Excel");
                throw;
            }
        }

        /// <summary>
        /// Exporta reporte de asistencia semanal con totales de horas trabajadas y horas extras por semana
        /// </summary>
        public async Task<byte[]> ExportWeeklyAttendanceReportAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("Iniciando exportación de Reporte de Asistencia Semanal para período {FechaInicio} - {FechaFin}",
                filter.FechaInicio, filter.FechaFin);

            try
            {
                // 1. Obtener datos del SP
                var data = await GetAttendanceNoPaginatedAsync(filter);

                if (!data.Success || !data.Data.Any())
                {
                    throw new InvalidOperationException("No hay datos para exportar");
                }

                // 2. Procesar datos y hacer pivot
                var pivotData = ProcessDataForPivot(data.Data, filter.FechaInicio, filter.FechaFin);

                // 3. Generar Excel con reporte de Asistencia Semanal
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Asistencia Semanal");

                // 4. Configurar formato y generar contenido
                GenerateWeeklyAttendanceReport(worksheet, pivotData, filter.FechaInicio, filter.FechaFin);

                // 5. Convertir a bytes
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var result = stream.ToArray();

                _logger.LogInformation("Exportación de Asistencia Semanal completada. {TotalEmployees} empleados, {FileSize} bytes",
                    pivotData.Count, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de asistencia semanal a Excel");
                throw;
            }
        }

        private async Task<AttendanceMatrixResponseDto> GetAttendanceNoPaginatedAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            var response = new AttendanceMatrixResponseDto
            {
                Data = new List<AttendanceMatrixDto>(),
                GeneratedAt = startTime
            };

            try
            {
                _logger.LogInformation("Iniciando consulta de matriz de asistencia para período {FechaInicio} - {FechaFin}",
                    filter.FechaInicio, filter.FechaFin);

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("sp_AttendanceMatrixOptimized_Excel", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300 // 5 minutos timeout
                };

                // Agregar parámetros
                AddParameterPivot(command, filter);

                await connection.OpenAsync();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToDto2(reader));
                }

                response.TotalRecords = response.Data.Count();
                //response.CurrentPage = response.Data[0].CurrentPage ?? 0;
                //response.PageSize = response.Data[0].PageSize ?? 0;
                //response.TotalPages = response.Data[0].TotalPages ?? 0.0;
                response.Success = true;
                response.Message = $"Consulta exitosa. {response.TotalRecords} registros encontrados.";

                _logger.LogInformation("Consulta completada exitosamente. {TotalRecords} registros obtenidos",
                    response.TotalRecords);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al ejecutar consulta de matriz de asistencia");
                response.Success = false;
                response.Message = $"Error en base de datos: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener matriz de asistencia");
                response.Success = false;
                response.Message = $"Error interno: {ex.Message}";
            }

            response.ExecutionTime = DateTime.Now - startTime;
            return response;
        }


        /// <summary>
        /// Procesa los datos verticales y los convierte en formato pivot horizontal
        /// </summary>
        private List<EmployeePivotData> ProcessDataForPivot(List<AttendanceMatrixDto> rawData, DateTime fechaInicio, DateTime fechaFin)
        {
            _logger.LogInformation("Procesando {TotalRecords} registros para pivot", rawData.Count);

            var employeeGroups = rawData
                .GroupBy(x => new { x.PersonalId, x.NroDoc, x.Colaborador })
                .Select(employeeGroup =>
                {
                    var firstRecord = employeeGroup.First();
                    var dailyData = new Dictionary<DateTime, DailyAttendanceData>();

                    // Procesar cada día del empleado
                    foreach (var dayRecord in employeeGroup)
                    {
                        dailyData[dayRecord.Fecha.Date] = new DailyAttendanceData
                        {
                            DiaSemana = dayRecord.DiaSemanaEs,
                            TipoDia = dayRecord.TipoDia,
                            TurnoNombre = dayRecord.TurnoNombre,
                            EntradaProgramada = dayRecord.EntradaProgramada,
                            SalidaProgramada = dayRecord.SalidaProgramada,
                            MarcacionesDelDia = dayRecord.MarcacionesDelDia,
                            OrigenMarcaciones = dayRecord.OrigenMarcaciones,
                            TipoPermiso = dayRecord.TipoPermiso,
                            EntradaReal = ExtractFirstTime(dayRecord.MarcacionesDelDia, dayRecord.TipoPermiso),
                            SalidaReal = ExtractLastTime(dayRecord.MarcacionesDelDia, dayRecord.TipoPermiso)
                        };
                    }

                    // Calcular totales
                    var totalHoras = CalculateTotalHours(dailyData, fechaInicio, fechaFin);
                    var horasExtras = CalculateOvertimeHours(dailyData, fechaInicio, fechaFin);

                    return new EmployeePivotData
                    {
                        PersonalId = firstRecord.PersonalId,
                        NroDoc = firstRecord.NroDoc,
                        Colaborador = firstRecord.Colaborador,
                        Sede = firstRecord.Sede,
                        Area = firstRecord.Area,
                        Cargo = firstRecord.Cargo,
                        CentroCosto = firstRecord.CentroCosto,
                        CCCodigo=firstRecord.CcCodigo,
                        Compania = firstRecord.Compania,
                        Planilla = firstRecord.Planilla,
                        FechaIngreso = firstRecord.FechaIngreso,
                        DailyData = dailyData,
                        TotalHoras = totalHoras,
                        HorasExtras = horasExtras
                    };
                })
                .OrderBy(x => x.Colaborador)
                .ToList();

            _logger.LogInformation("Pivot procesado: {TotalEmployees} empleados", employeeGroups.Count);
            return employeeGroups;
        }

        /// <summary>
        /// Genera el reporte de Centro de Costos con formato específico
        /// </summary>
        private void GenerateCostCenterReport(IXLWorksheet worksheet, List<EmployeePivotData> pivotData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            var currentRow = 1;
            var currentCol = 1;

            // Generar lista de fechas del período
            var dateRange = GenerateDateRange(fechaInicio, fechaFin);
            var weekGroups = GenerateWeekGroups(dateRange);

            // === TÍTULO PRINCIPAL ===
            worksheet.Cell(currentRow, 1).Value = "REPORTE DE CENTRO DE COSTO - "+fechaInicio.ToString("yyyy-MM-dd") +"-"+fechaFin.ToString("yyyy-MM-dd");
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            
            // Merge del título
            var titleRange = worksheet.Range(currentRow, 1, currentRow, 7 + (dateRange.Count));
            titleRange.Merge();
            titleRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow += 2;

            // === ENCABEZADOS PRINCIPALES ===
            var headers = new List<string>
            {
                "ITEM", "PLANILLA", "NRO DOC", "APELLIDOS Y NOMBRES", 
                "AREA", "CARGO", "FECHA INGRESO"
            };

            // Agregar encabezados fijos
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
                worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            currentCol = headers.Count + 1;

            // === ENCABEZADOS DE SEMANAS ===
            foreach (var week in weekGroups)
            {
                // Encabezado SEMANA N°
                var weekHeaderRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + week.Dates.Count);
                weekHeaderRange.Merge();
                worksheet.Cell(currentRow, currentCol).Value = $"SEMANA N° {week.WeekNumber}";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentCol += week.Dates.Count + 1; // +1 para la columna TURNO
            }

            currentRow++;

            // === SUB-ENCABEZADOS (DÍAS DE LA SEMANA) ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Columna TURNO
                worksheet.Cell(currentRow, currentCol).Value = "TURNO";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                currentCol++;

                // Días de la semana con nombre del día
                foreach (var date in week.Dates)
                {
                    var dayName = GetSpanishDayName(date.DayOfWeek);
                    worksheet.Cell(currentRow, currentCol).Value = dayName;
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    currentCol++;
                }
            }

            currentRow++;

            // === TERCERA FILA: NÚMEROS DE DÍA ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Columna TURNO (vacía en esta fila)
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                currentCol++;

                // Números de día
                foreach (var date in week.Dates)
                {
                    worksheet.Cell(currentRow, currentCol).Value = date.Day.ToString("00");
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    currentCol++;
                }
            }

            currentRow++;

            // === DATOS DE EMPLEADOS ===
            var itemNumber = 1;
            foreach (var employee in pivotData)
            {
                currentCol = 1;

                // Datos fijos del empleado
                worksheet.Cell(currentRow, currentCol++).Value = itemNumber++;
                worksheet.Cell(currentRow, currentCol++).Value = GetAbbreviatedPlanilla(employee.Planilla); // Usar planilla real de BD
                worksheet.Cell(currentRow, currentCol++).Value = employee.NroDoc;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Colaborador;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Area;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Cargo;
                worksheet.Cell(currentRow, currentCol++).Value = employee.FechaIngreso;

                // Datos por semana
                foreach (var week in weekGroups)
                {
                    // Columna TURNO (obtener el turno más común de la semana)
                    var weekTurno = GetWeekTurno(employee, week.Dates);
                    worksheet.Cell(currentRow, currentCol++).Value = weekTurno;

                    // Datos por día
                    foreach (var date in week.Dates)
                    {
                        var cellValue = GetCostCenterValue(employee, date);
                        worksheet.Cell(currentRow, currentCol++).Value = cellValue;
                    }
                }

                // Alternar color de filas
                if (currentRow % 2 == 0)
                {
                    var range = worksheet.Range(currentRow, 1, currentRow, currentCol - 1);
                    range.Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }

                currentRow++;
            }

            // === FORMATEO FINAL ===
            ApplyCostCenterFormatting(worksheet, currentRow - 1, currentCol - 1);

            _logger.LogInformation("Reporte Centro de Costos generado con {TotalRows} filas y {TotalCols} columnas",
                currentRow - 1, currentCol - 1);
        }

        /// <summary>
        /// Genera el reporte de Marcaciones con formato específico
        /// </summary>
        private void GenerateMarkingsReport(IXLWorksheet worksheet, List<EmployeePivotData> pivotData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            var currentRow = 1;
            var currentCol = 1;

            // Generar lista de fechas del período
            var dateRange = GenerateDateRange(fechaInicio, fechaFin);
            var weekGroups = GenerateWeekGroups(dateRange);

            // === TÍTULO PRINCIPAL ===
            worksheet.Cell(currentRow, 1).Value = "REPORTE DE MARCACIONES - " + fechaInicio.ToString("yyyy-MM-dd") + "-" + fechaFin.ToString("yyyy-MM-dd");
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Merge del título
            var titleRange = worksheet.Range(currentRow, 1, currentRow, 7 + (dateRange.Count));
            titleRange.Merge();
            titleRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow += 2;

            // === ENCABEZADOS PRINCIPALES ===
            var headers = new List<string>
            {
                "ITEM", "PLANILLA", "NRO DOC", "APELLIDOS Y NOMBRES",
                "AREA", "CARGO", "FECHA INGRESO"
            };

            // Agregar encabezados fijos
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
                worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            currentCol = headers.Count + 1;

            // === ENCABEZADOS DE SEMANAS ===
            foreach (var week in weekGroups)
            {
                // Encabezado SEMANA N°
                var weekHeaderRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + week.Dates.Count);
                weekHeaderRange.Merge();
                worksheet.Cell(currentRow, currentCol).Value = $"SEMANA N° {week.WeekNumber}";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentCol += week.Dates.Count + 1; // +1 para la columna TURNO
            }

            currentRow++;

            // === SUB-ENCABEZADOS (DÍAS DE LA SEMANA) ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Columna TURNO
                worksheet.Cell(currentRow, currentCol).Value = "TURNO";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                currentCol++;

                // Días de la semana con nombre del día
                foreach (var date in week.Dates)
                {
                    var dayName = GetSpanishDayName(date.DayOfWeek);
                    worksheet.Cell(currentRow, currentCol).Value = dayName;
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    currentCol++;
                }
            }

            currentRow++;

            // === TERCERA FILA: NÚMEROS DE DÍA ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Columna TURNO (vacía en esta fila)
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                currentCol++;

                // Números de día
                foreach (var date in week.Dates)
                {
                    worksheet.Cell(currentRow, currentCol).Value = date.Day.ToString("00");
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, currentCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    currentCol++;
                }
            }

            currentRow++;

            // === DATOS DE EMPLEADOS ===
            var itemNumber = 1;
            foreach (var employee in pivotData)
            {
                currentCol = 1;

                // Datos fijos del empleado
                worksheet.Cell(currentRow, currentCol++).Value = itemNumber++;
                worksheet.Cell(currentRow, currentCol++).Value = GetAbbreviatedPlanilla(employee.Planilla);
                worksheet.Cell(currentRow, currentCol++).Value = employee.NroDoc;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Colaborador;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Area;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Cargo;
                worksheet.Cell(currentRow, currentCol++).Value = employee.FechaIngreso;

                // Datos por semana
                foreach (var week in weekGroups)
                {
                    // Columna TURNO (obtener el turno más común de la semana)
                    var weekTurno = GetWeekTurno(employee, week.Dates);
                    worksheet.Cell(currentRow, currentCol++).Value = weekTurno;

                    // Datos por día - CANTIDAD DE MARCACIONES
                    foreach (var date in week.Dates)
                    {
                        var cellValue = GetMarkingsValue(employee, date);
                        worksheet.Cell(currentRow, currentCol++).Value = cellValue;
                    }
                }

                // Alternar color de filas
                if (currentRow % 2 == 0)
                {
                    var range = worksheet.Range(currentRow, 1, currentRow, currentCol - 1);
                    range.Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }

                currentRow++;
            }

            // === FORMATEO FINAL ===
            ApplyMarkingsFormatting(worksheet, currentRow - 1, currentCol - 1);

            _logger.LogInformation("Reporte de Marcaciones generado con {TotalRows} filas y {TotalCols} columnas",
                currentRow - 1, currentCol - 1);
        }

        /// <summary>
        /// Genera el reporte de Asistencia Semanal con totales por semana
        /// </summary>
        private void GenerateWeeklyAttendanceReport(IXLWorksheet worksheet, List<EmployeePivotData> pivotData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            var currentRow = 1;
            var currentCol = 1;

            // Generar lista de fechas del período
            var dateRange = GenerateDateRange(fechaInicio, fechaFin);
            var weekGroups = GenerateWeekGroups(dateRange);

            // === TÍTULO PRINCIPAL ===
            worksheet.Cell(currentRow, 1).Value = "REPORTE DE ASISTENCIA SEMANAL - " + fechaInicio.ToString("yyyy-MM-dd") + "-" + fechaFin.ToString("yyyy-MM-dd");
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Calcular el número total de columnas para el merge del título (dinámico)
            // Cada semana: días reales * 2 columnas (E/S) + 2 totales
            var totalColumns = 8; // headers fijos
            foreach (var week in weekGroups)
            {
                totalColumns += (week.Dates.Count * 2) + 2; // días reales * 2 + totales semanales
            }
            totalColumns += 2; // totales globales
            var titleRange = worksheet.Range(currentRow, 1, currentRow, totalColumns);
            titleRange.Merge();
            titleRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            currentRow += 2;

            // === ENCABEZADOS PRINCIPALES ===
            var headers = new List<string>
            {
                "ITEM", "PLANILLA", "NRO DOC", "APELLIDOS Y NOMBRES",
                "AREA", "CENTRO COSTO", "CARGO", "FECHA INGRESO"
            };

            // Agregar encabezados fijos
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
                worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(currentRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            currentCol = headers.Count + 1;

            // === ENCABEZADOS DE SEMANAS CON TOTALES ===
            foreach (var week in weekGroups)
            {
                // Encabezado SEMANA N° (dinámico: días reales * 2 + 2 totales)
                var weekColumns = (week.Dates.Count * 2) + 2; // días reales * 2 + totales
                var weekHeaderRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + weekColumns - 1);
                weekHeaderRange.Merge();
                worksheet.Cell(currentRow, currentCol).Value = $"SEMANA N° {week.WeekNumber}";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentCol += weekColumns; // Avanzar solo las columnas necesarias para esta semana
            }

            // === ENCABEZADO TOTALES GLOBALES ===
            var globalTotalsRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + 1);
            globalTotalsRange.Merge();
            worksheet.Cell(currentRow, currentCol).Value = "TOTALES GLOBALES";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
            worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow++;

            // === SUB-ENCABEZADOS (DÍAS DE LA SEMANA + TOTALES) ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Solo procesar los días que realmente existen en la semana
                foreach (var date in week.Dates)
                {
                    var dayName = GetSpanishDayName(date.DayOfWeek);
                    // Fusionar 2 columnas para el nombre del día
                    var dayRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + 1);
                    dayRange.Merge();
                    worksheet.Cell(currentRow, currentCol).Value = dayName;
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol += 2; // Avanzar 2 columnas por día real
                }

                // Totales semanales (2 columnas)
                worksheet.Cell(currentRow, currentCol).Value = "H. TRAB";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                currentCol++;

                worksheet.Cell(currentRow, currentCol).Value = "H. EXTRA";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                currentCol++;
            }

            // Totales globales
            worksheet.Cell(currentRow, currentCol).Value = "TOTAL H.";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            worksheet.Cell(currentRow, currentCol).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentCol++;

            worksheet.Cell(currentRow, currentCol).Value = "TOTAL E.";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            worksheet.Cell(currentRow, currentCol).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow++;

            // === TERCERA FILA: NÚMEROS DE DÍA ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Solo procesar números de los días que realmente existen
                foreach (var date in week.Dates)
                {
                    // Fusionar 2 columnas para el número del día
                    var dayRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + 1);
                    dayRange.Merge();
                    worksheet.Cell(currentRow, currentCol).Value = date.Day.ToString("00");
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol += 2; // Avanzar 2 columnas por día real
                }

                // Espacios para totales semanales (vacíos)
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                currentCol++;
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                currentCol++;
            }

            // Espacios para totales globales
            worksheet.Cell(currentRow, currentCol).Value = "";
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            currentCol++;
            worksheet.Cell(currentRow, currentCol).Value = "";
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;

            currentRow++;

            // === CUARTA FILA: ENTRADA/SALIDA ===
            currentCol = headers.Count + 1;
            foreach (var week in weekGroups)
            {
                // Entrada/Salida solo para los días que realmente existen
                foreach (var date in week.Dates)
                {
                    // Columna Entrada
                    worksheet.Cell(currentRow, currentCol).Value = "ENTRADA";
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.Wheat;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol++;
                    
                    // Columna Salida
                    worksheet.Cell(currentRow, currentCol).Value = "SALIDA";
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.Wheat;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol++;
                }

                // Espacios para totales semanales
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                currentCol++;
                worksheet.Cell(currentRow, currentCol).Value = "";
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                currentCol++;
            }

            // Espacios para totales globales
            worksheet.Cell(currentRow, currentCol).Value = "";
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            currentCol++;
            worksheet.Cell(currentRow, currentCol).Value = "";
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;

            currentRow++;

            // === DATOS DE EMPLEADOS ===
            var itemNumber = 1;
            foreach (var employee in pivotData)
            {
                currentCol = 1;

                // Datos fijos del empleado
                worksheet.Cell(currentRow, currentCol++).Value = itemNumber++;
                worksheet.Cell(currentRow, currentCol++).Value = GetAbbreviatedPlanilla(employee.Planilla);
                worksheet.Cell(currentRow, currentCol++).Value = employee.NroDoc;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Colaborador;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Area;
                worksheet.Cell(currentRow, currentCol++).Value = employee.CCCodigo; // Centro de Costo
                worksheet.Cell(currentRow, currentCol++).Value = employee.Cargo;
                worksheet.Cell(currentRow, currentCol++).Value = employee.FechaIngreso;

                // Calcular totales globales del empleado
                decimal totalGlobalHoras = 0;
                decimal totalGlobalExtras = 0;

                // Datos por semana
                foreach (var week in weekGroups)
                {
                    decimal weekTotalHours = 0;
                    decimal weekOvertimeHours = 0;

                    // Procesar solo los días que realmente existen en la semana
                    foreach (var date in week.Dates)
                    {
                        if (employee.DailyData.TryGetValue(date.Date, out var dayData))
                        {
                            // Crear celdas de entrada y salida
                            var entradaCell = worksheet.Cell(currentRow, currentCol);
                            var salidaCell = worksheet.Cell(currentRow, currentCol + 1);

                            // Mostrar entrada y salida
                            entradaCell.Value = dayData.EntradaReal ?? "";
                            salidaCell.Value = dayData.SalidaReal ?? "";

                            // Aplicar colores según el estado
                            ApplyWeeklyAttendanceCellColor(entradaCell, dayData, "entrada");
                            ApplyWeeklyAttendanceCellColor(salidaCell, dayData, "salida");

                            currentCol += 2;

                            // Calcular horas trabajadas del día
                            var dailyHours = CalculateDailyWorkedHours(dayData);
                            weekTotalHours += dailyHours;

                            // Calcular horas extras (si trabajó más de 8 horas)
                            if (dailyHours > 8)
                            {
                                weekOvertimeHours += dailyHours - 8;
                            }
                        }
                        else
                        {
                            // Día sin datos - pintar de rojo
                            var entradaCell = worksheet.Cell(currentRow, currentCol);
                            var salidaCell = worksheet.Cell(currentRow, currentCol + 1);
                            
                            entradaCell.Value = "";
                            salidaCell.Value = "";
                            
                            // Aplicar color rojo para días sin datos
                            entradaCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                            salidaCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                            
                            currentCol += 2;
                        }
                    }

                    // Mostrar totales semanales
                    worksheet.Cell(currentRow, currentCol).Value = Math.Round(weekTotalHours, 1);
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol++;

                    worksheet.Cell(currentRow, currentCol).Value = Math.Round(weekOvertimeHours, 1);
                    worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    currentCol++;

                    // Acumular en totales globales
                    totalGlobalHoras += weekTotalHours;
                    totalGlobalExtras += weekOvertimeHours;
                }

                // Mostrar totales globales
                worksheet.Cell(currentRow, currentCol).Value = Math.Round(totalGlobalHoras, 1);
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
                worksheet.Cell(currentRow, currentCol).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                currentCol++;

                worksheet.Cell(currentRow, currentCol).Value = Math.Round(totalGlobalExtras, 1);
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.DarkGreen;
                worksheet.Cell(currentRow, currentCol).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Alternar color de filas
                if (currentRow % 2 == 0)
                {
                    var range = worksheet.Range(currentRow, 1, currentRow, currentCol);
                    range.Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }

                currentRow++;
            }

            // === FORMATEO FINAL ===
            ApplyWeeklyAttendanceFormatting(worksheet, currentRow - 1, currentCol);

            _logger.LogInformation("Reporte de Asistencia Semanal generado con {TotalRows} filas y {TotalCols} columnas",
                currentRow - 1, currentCol);
        }

        /// <summary>
        /// Genera el contenido del Excel con formato similar al archivo original
        /// </summary>
        private void GenerateExcelContent(IXLWorksheet worksheet, List<EmployeePivotData> pivotData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            var currentRow = 1;
            var currentCol = 1;

            // Generar lista de fechas del período
            var dateRange = GenerateDateRange(fechaInicio, fechaFin);

            // === ENCABEZADOS PRINCIPALES ===

            // Fila 1: Título del período
            worksheet.Cell(currentRow, 10).Value = $"PERIODO\r\n{fechaInicio:dd-MM} AL {fechaFin:dd-MM-yy}";
            worksheet.Cell(currentRow, 10).Style.Alignment.WrapText = true;
            worksheet.Cell(currentRow, 10).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow = 2;

            // Fila 2: Encabezados fijos + días
            var headers = new List<string>
    {
        "TIPO DOC.", "N° DOC.", "COLABORADOR", "SEDE", "AREA", "CARGO",
        "PERSONAL NO FISCALIZADO", "CC", "FECHA INGRESO"
    };

            // Agregar encabezados fijos
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
                worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            currentCol = headers.Count + 1;

            // Agregar encabezados de fechas
            foreach (var date in dateRange)
            {
                // FUSIONAR 2 COLUMNAS PARA EL DÍA
                var dayHeaderRange = worksheet.Range(currentRow, currentCol, currentRow, currentCol + 1);
                dayHeaderRange.Merge();

                worksheet.Cell(currentRow, currentCol).Value = date.ToString("dddd", new CultureInfo("es-ES")).ToUpper();
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                currentCol += 2; // Avanzar 2 columnas (ENTRADA y SALIDA)
            }

            // Columnas de totales (sin cambios)
            worksheet.Cell(currentRow, currentCol).Value = "TOTAL HORAS";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;
            currentCol++;

            worksheet.Cell(currentRow, currentCol).Value = "HORAS EXTRAS";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGreen;

            currentRow = 3;

            // Fila 3: Sub-encabezados (ENTRADA/SALIDA para cada día)
            currentCol = headers.Count + 1;
            foreach (var date in dateRange)
            {
                worksheet.Cell(currentRow, currentCol).Value = "ENTRADA";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                currentCol++;

                worksheet.Cell(currentRow, currentCol).Value = "SALIDA";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightYellow;
                currentCol++;
            }

            currentRow = 4;

            // === DATOS DE EMPLEADOS ===
            foreach (var employee in pivotData)
            {
                currentCol = 1;

                // Datos fijos del empleado
                worksheet.Cell(currentRow, currentCol++).Value = "DNI";
                worksheet.Cell(currentRow, currentCol++).Value = employee.NroDoc;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Colaborador;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Sede;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Area;
                worksheet.Cell(currentRow, currentCol++).Value = employee.Cargo;
                worksheet.Cell(currentRow, currentCol++).Value = ""; // Personal no fiscalizado
                worksheet.Cell(currentRow, currentCol++).Value = employee.CCCodigo;
                worksheet.Cell(currentRow, currentCol++).Value = employee.FechaIngreso;

                // Datos por día
                foreach (var date in dateRange)
                {
                    if (employee.DailyData.TryGetValue(date.Date, out var dayData))
                    {
                        // DEBUG: Ver qué se está escribiendo

                        worksheet.Cell(currentRow, currentCol++).Value = dayData.EntradaReal ?? "";
                        worksheet.Cell(currentRow, currentCol++).Value = dayData.SalidaReal ?? "";
                    }
                    else
                    {

                        worksheet.Cell(currentRow, currentCol++).Value = "";
                        worksheet.Cell(currentRow, currentCol++).Value = "";
                    }
                }

                // Totales
                worksheet.Cell(currentRow, currentCol++).Value = employee.TotalHoras;
                worksheet.Cell(currentRow, currentCol).Value = employee.HorasExtras;

                // Alternar color de filas
                if (currentRow % 2 == 0)
                {
                    var range = worksheet.Range(currentRow, 1, currentRow, currentCol);
                    range.Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }

                currentRow++;
            }

            // === FORMATEO FINAL ===

            // Ajustar anchos de columna
            worksheet.Column(1).Width = 10;  // TIPO DOC
            worksheet.Column(2).Width = 12;  // N° DOC
            worksheet.Column(3).Width = 35;  // COLABORADOR
            worksheet.Column(4).Width = 15;  // SEDE
            worksheet.Column(5).Width = 20;  // AREA
            worksheet.Column(6).Width = 25;  // CARGO
            worksheet.Column(7).Width = 12;  // PERSONAL NO FISCALIZADO
            worksheet.Column(8).Width = 20;  // CC
            worksheet.Column(9).Width = 12;  // FECHA INGRESO

            // Columnas de días
            for (int i = 10; i <= 9 + (dateRange.Count * 2); i++)
            {
                worksheet.Column(i).Width = 8;
            }

            // Columnas de totales
            worksheet.Column(9 + (dateRange.Count * 2) + 1).Width = 12;
            worksheet.Column(9 + (dateRange.Count * 2) + 2).Width = 12;

            // Congelar filas y columnas
            worksheet.SheetView.FreezeRows(3);
            worksheet.SheetView.FreezeColumns(3);

            // Agregar bordes
            var dataRange = worksheet.Range(2, 1, currentRow - 1, 9 + (dateRange.Count * 2) + 2);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            _logger.LogInformation("Excel generado con {TotalRows} filas y {TotalCols} columnas",
                currentRow - 1, 9 + (dateRange.Count * 2) + 2);
        }

        /// <summary>
        /// Genera lista de fechas en el rango especificado
        /// </summary>
        private List<DateTime> GenerateDateRange(DateTime fechaInicio, DateTime fechaFin)
        {
            var dates = new List<DateTime>();
            for (var date = fechaInicio.Date; date <= fechaFin.Date; date = date.AddDays(1))
            {
                dates.Add(date);
            }
            return dates;
        }
        /// <summary>
        /// Extrae la primera hora única
        /// </summary>
        private string ExtractFirstTime(string marcaciones, string tipoPermiso)
        {
            if ((string.IsNullOrEmpty(marcaciones) || marcaciones == "SIN_MARCACIONES") &&
                !string.IsNullOrEmpty(tipoPermiso))
                return tipoPermiso;

            if (string.IsNullOrEmpty(marcaciones) || marcaciones == "SIN_MARCACIONES")
                return "FALTA";

            var horasUnicas = marcaciones
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(m =>
                {
                    var limpia = m.Trim();
                    var parenIndex = limpia.IndexOf('(');
                    return parenIndex > 0 ? limpia.Substring(0, parenIndex).Trim() : limpia.Trim();
                })
                .Distinct()
                .ToList();

            return horasUnicas.FirstOrDefault() ?? "FALTA";
        }

        /// <summary>
        /// Extrae la última hora única
        /// </summary>
        private string ExtractLastTime(string marcaciones, string tipoPermiso)
        {
            if ((string.IsNullOrEmpty(marcaciones) || marcaciones == "SIN_MARCACIONES") &&
                !string.IsNullOrEmpty(tipoPermiso))
                return "";

            if (string.IsNullOrEmpty(marcaciones) || marcaciones == "SIN_MARCACIONES")
                return "FALTA";

            var horasUnicas = marcaciones
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(m =>
                {
                    var limpia = m.Trim();
                    var parenIndex = limpia.IndexOf('(');
                    return parenIndex > 0 ? limpia.Substring(0, parenIndex).Trim() : limpia.Trim();
                })
                .Distinct()
                .ToList();

            return horasUnicas.LastOrDefault() ?? "FALTA";
        }


        /// <summary>
        /// Calcula total de horas trabajadas (lógica corregida)
        /// </summary>
        private decimal CalculateTotalHours(Dictionary<DateTime, DailyAttendanceData> dailyData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            decimal totalHours = 0;

            for (var date = fechaInicio.Date; date <= fechaFin.Date; date = date.AddDays(1))
            {
                if (dailyData.TryGetValue(date, out var dayData))
                {
                    // Si hay permiso, no contar horas
                    if (!string.IsNullOrEmpty(dayData.TipoPermiso))
                        continue;

                    // Si es FALTA, continuar con el siguiente día (no sumar, pero no parar)
                    if (dayData.EntradaReal == "FALTA" || dayData.SalidaReal == "FALTA")
                        continue;

                    // Solo calcular si hay entrada y salida válidas (horarios reales)
                    if (!string.IsNullOrEmpty(dayData.EntradaReal) &&
                        !string.IsNullOrEmpty(dayData.SalidaReal) &&
                        TimeSpan.TryParse(dayData.EntradaReal, out var entrada) &&
                        TimeSpan.TryParse(dayData.SalidaReal, out var salida))
                    {
                        var workedHours = salida - entrada;
                        if (workedHours.TotalHours > 0 && workedHours.TotalHours < 24)
                        {
                            totalHours += (decimal)workedHours.TotalHours;
                        }
                    }
                }
            }

            return Math.Round(totalHours, 2);
        }


        /// <summary>
        /// Calcula horas extras (lógica corregida)
        /// </summary>
        private decimal CalculateOvertimeHours(Dictionary<DateTime, DailyAttendanceData> dailyData,
            DateTime fechaInicio, DateTime fechaFin)
        {
            decimal overtimeHours = 0;
            const decimal normalWorkDay = 8.0m;

            for (var date = fechaInicio.Date; date <= fechaFin.Date; date = date.AddDays(1))
            {
                if (dailyData.TryGetValue(date, out var dayData))
                {
                    // Si hay permiso, no contar horas extras
                    if (!string.IsNullOrEmpty(dayData.TipoPermiso))
                        continue;

                    // Si es FALTA, continuar con el siguiente día
                    if (dayData.EntradaReal == "FALTA" || dayData.SalidaReal == "FALTA")
                        continue;

                    // Solo calcular si hay entrada y salida válidas
                    if (!string.IsNullOrEmpty(dayData.EntradaReal) &&
                        !string.IsNullOrEmpty(dayData.SalidaReal) &&
                        TimeSpan.TryParse(dayData.EntradaReal, out var entrada) &&
                        TimeSpan.TryParse(dayData.SalidaReal, out var salida))
                    {
                        var workedHours = (decimal)(salida - entrada).TotalHours;
                        if (workedHours > normalWorkDay && workedHours < 24)
                        {
                            overtimeHours += workedHours - normalWorkDay;
                        }
                    }
                }
            }

            return Math.Round(overtimeHours, 2);
        }

        /// <summary>
        /// Agrega parámetros al comando SQL
        /// </summary>
        private void AddParameters(SqlCommand command, AttendanceMatrixFilterDto filter)
        {
            command.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = filter.FechaInicio;
            command.Parameters.Add("@FechaFin", SqlDbType.Date).Value = filter.FechaFin;

            command.Parameters.Add("@EmployeeId", SqlDbType.VarChar, 20).Value =
                string.IsNullOrEmpty(filter.EmployeeId) ? DBNull.Value : filter.EmployeeId;

            command.Parameters.Add("@CompaniaId", SqlDbType.VarChar, 2).Value =
                string.IsNullOrEmpty(filter.CompaniaId) ? DBNull.Value : filter.CompaniaId;

            command.Parameters.Add("@AreaId", SqlDbType.VarChar, 3).Value =
                string.IsNullOrEmpty(filter.AreaId) ? DBNull.Value : filter.AreaId;

            command.Parameters.Add("@SedeId", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.SedeId) ? DBNull.Value : filter.SedeId;

            command.Parameters.Add("@CargoId", SqlDbType.VarChar, 3).Value =
                string.IsNullOrEmpty(filter.CargoId) ? DBNull.Value : filter.CargoId;

            command.Parameters.Add("@CentroCostoId", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.CentroCostoId) ? DBNull.Value : filter.CentroCostoId;

            command.Parameters.Add("@SedeCodigo", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.SedeCodigo) ? DBNull.Value : filter.SedeCodigo;

            command.Parameters.Add("@CCCodigo", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.CcCodigo) ? DBNull.Value : filter.CcCodigo;

            command.Parameters.Add("@PageNumber", SqlDbType.VarChar, 15).Value =
               string.IsNullOrEmpty(filter.PageNumber.ToString()) ? DBNull.Value : filter.PageNumber;

            command.Parameters.Add("@PageSize", SqlDbType.VarChar, 15).Value =
               string.IsNullOrEmpty(filter.PageSize.ToString()) ? DBNull.Value : filter.PageSize;

            command.Parameters.Add("@PlanillaId", SqlDbType.VarChar, 15).Value =
               string.IsNullOrEmpty(filter.PlanillaId) ? DBNull.Value : filter.PlanillaId;
        }
        private void AddParameterPivot(SqlCommand command, AttendanceMatrixFilterDto filter)
        {
            command.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = filter.FechaInicio;
            command.Parameters.Add("@FechaFin", SqlDbType.Date).Value = filter.FechaFin;

            command.Parameters.Add("@EmployeeId", SqlDbType.VarChar, 20).Value =
                string.IsNullOrEmpty(filter.EmployeeId) ? DBNull.Value : filter.EmployeeId;

            command.Parameters.Add("@CompaniaId", SqlDbType.VarChar, 2).Value =
                string.IsNullOrEmpty(filter.CompaniaId) ? DBNull.Value : filter.CompaniaId;

            command.Parameters.Add("@AreaId", SqlDbType.VarChar, 3).Value =
                string.IsNullOrEmpty(filter.AreaId) ? DBNull.Value : filter.AreaId;

            command.Parameters.Add("@SedeId", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.SedeId) ? DBNull.Value : filter.SedeId;

            command.Parameters.Add("@CargoId", SqlDbType.VarChar, 3).Value =
                string.IsNullOrEmpty(filter.CargoId) ? DBNull.Value : filter.CargoId;

            command.Parameters.Add("@CentroCostoId", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.CentroCostoId) ? DBNull.Value : filter.CentroCostoId;

            command.Parameters.Add("@SedeCodigo", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.SedeCodigo) ? DBNull.Value : filter.SedeCodigo;

            command.Parameters.Add("@CCCodigo", SqlDbType.VarChar, 15).Value =
                string.IsNullOrEmpty(filter.CcCodigo) ? DBNull.Value : filter.CcCodigo;

            command.Parameters.Add("@PlanillaId", SqlDbType.VarChar, 2).Value =
               string.IsNullOrEmpty(filter.PlanillaId) ? DBNull.Value : filter.PlanillaId;
        }

        /// <summary>
        /// Mapea SqlDataReader a DTO
        /// </summary>
        private AttendanceMatrixDto MapToDto(SqlDataReader reader)
        {
            return new AttendanceMatrixDto
            {
                PersonalId = GetSafeString(reader, "Personal_Id"),
                NroDoc = GetSafeString(reader, "Nro_Doc"),
                Colaborador = GetSafeString(reader, "colaborador"),
                Sede = GetSafeString(reader, "sede"),
                SedeCodigo = GetSafeString(reader, "sede_codigo"),
                Area = GetSafeString(reader, "area"),
                Cargo = GetSafeString(reader, "cargo"),
                CentroCosto = GetSafeString(reader, "centro_costo"),
                CcCodigo = GetSafeString(reader, "cc_codigo"),
                Compania = GetSafeString(reader, "compania"),
                FechaIngreso = GetSafeString(reader, "fecha_ingreso"),

                // Información del día
                Fecha = GetSafeDateTime(reader, "Fecha"),
                DiaSemanaEs = GetSafeString(reader, "dia_semana_es"),

                // Configuración de horario
                TurnoNombre = GetSafeString(reader, "turno_nombre"),
                TipoHorario = GetSafeString(reader, "tipo_horario"),
                TipoDia = GetSafeString(reader, "tipo_dia"),

                // Horarios programados
                EntradaProgramada = GetSafeString(reader, "entrada_programada"),
                SalidaProgramada = GetSafeString(reader, "salida_programada"),
                MarcacionesEsperadas = GetSafeInt(reader, "marcaciones_esperadas"),
                BreaksConfigurados = GetSafeString(reader, "breaks_configurados"),

                // Permisos
                TipoPermiso = GetSafeString(reader, "tipo_permiso"),

                // Marcaciones reales - NUEVOS CAMPOS
                MarcacionesDelDia = GetSafeString(reader, "marcaciones_del_dia"),
                MarcacionesManuales = GetSafeString(reader, "marcaciones_manuales"),
                RazonesManuales = GetSafeString(reader, "razones_manuales"),
                OrigenMarcaciones = GetSafeString(reader, "origen_marcaciones"),
                Planilla = GetSafeString(reader, "planilla"),

                //// datos de paginación
                TotalRecords = GetSafeInt(reader, "TotalRecords"),
                CurrentPage = GetSafeInt(reader, "CurrentPage"),
                PageSize = GetSafeInt(reader, "PageSize"),
                TotalPages = GetSafeDouble(reader, "TotalPages")

            };
        }

        private AttendanceMatrixDto MapToDto2(SqlDataReader reader)
        {
            return new AttendanceMatrixDto
            {
                PersonalId = GetSafeString(reader, "Personal_Id"),
                NroDoc = GetSafeString(reader, "Nro_Doc"),
                Colaborador = GetSafeString(reader, "colaborador"),
                Sede = GetSafeString(reader, "sede"),
                SedeCodigo = GetSafeString(reader, "sede_codigo"),
                Area = GetSafeString(reader, "area"),
                Cargo = GetSafeString(reader, "cargo"),
                CentroCosto = GetSafeString(reader, "centro_costo"),
                CcCodigo = GetSafeString(reader, "cc_codigo"),
                Compania = GetSafeString(reader, "compania"),
                FechaIngreso = GetSafeString(reader, "fecha_ingreso"),

                // Información del día
                Fecha = GetSafeDateTime(reader, "Fecha"),
                DiaSemanaEs = GetSafeString(reader, "dia_semana_es"),

                // Configuración de horario
                TurnoNombre = GetSafeString(reader, "turno_nombre"),
                TipoHorario = GetSafeString(reader, "tipo_horario"),
                TipoDia = GetSafeString(reader, "tipo_dia"),

                // Horarios programados
                EntradaProgramada = GetSafeString(reader, "entrada_programada"),
                SalidaProgramada = GetSafeString(reader, "salida_programada"),
                MarcacionesEsperadas = GetSafeInt(reader, "marcaciones_esperadas"),
                BreaksConfigurados = GetSafeString(reader, "breaks_configurados"),

                // Permisos
                TipoPermiso = GetSafeString(reader, "tipo_permiso"),

                // Marcaciones reales - NUEVOS CAMPOS
                MarcacionesDelDia = GetSafeString(reader, "marcaciones_del_dia"),
                MarcacionesManuales = GetSafeString(reader, "marcaciones_manuales"),
                RazonesManuales = GetSafeString(reader, "razones_manuales"),
                OrigenMarcaciones = GetSafeString(reader, "origen_marcaciones"),
                Planilla = GetSafeString(reader, "planilla"),

                //// datos de paginación
                //TotalRecords = GetSafeInt(reader, "TotalRecords"),
                //CurrentPage = GetSafeInt(reader, "CurrentPage"),
                //PageSize = GetSafeInt(reader, "PageSize"),
                //TotalPages = GetSafeDouble(reader, "TotalPages")

            };
        }

        /// <summary>
        /// Obtiene string de forma segura del SqlDataReader
        /// </summary>
        private string GetSafeString(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        /// <summary>
        /// Obtiene DateTime de forma segura del SqlDataReader
        /// </summary>
        private DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Obtiene int nullable de forma segura del SqlDataReader
        /// </summary>
        private int? GetSafeInt(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        private double? GetSafeDouble(SqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
        }

        /// <summary>
        /// Exporta reporte de Centro de Costos a Excel
        /// </summary>
        public async Task<byte[]> ExportCostCenterReportAsync(AttendanceMatrixFilterDto filter)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation("Iniciando exportación de Reporte Centro de Costos para período {FechaInicio} - {FechaFin}",
                filter.FechaInicio, filter.FechaFin);

            try
            {
                // 1. Obtener datos del SP
                var data = await GetAttendanceNoPaginatedAsync(filter);

                if (!data.Success || !data.Data.Any())
                {
                    throw new InvalidOperationException("No hay datos para exportar");
                }

                // 2. Procesar datos y hacer pivot
                var pivotData = ProcessDataForPivot(data.Data, filter.FechaInicio, filter.FechaFin);

                // 3. Generar Excel con reporte Centro de Costos
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Reporte Centro de Costos");

                // 4. Configurar formato y generar contenido
                GenerateCostCenterReport(worksheet, pivotData, filter.FechaInicio, filter.FechaFin);

                // 5. Convertir a bytes
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var result = stream.ToArray();

                _logger.LogInformation("Reporte Centro de Costos completado. {TotalEmployees} empleados, {FileSize} bytes",
                    pivotData.Count, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar Reporte Centro de Costos a Excel");
                throw;
            }
        }

        public async Task<AttendanceMatrixPivotResponseDto> GetAttendanceMatrixPivotAsync(AttendanceMatrixFilterDto filter)
        {
            try
            {
                _logger.LogInformation("Iniciando consulta pivot de matriz de asistencia para período {FechaInicio} - {FechaFin}",
                    filter.FechaInicio, filter.FechaFin);

                // Obtener datos sin paginación para el pivot
                var rawData = await GetAttendanceMatrixAsync(filter);

                if (!rawData.Success || !rawData.Data.Any())
                {
                    return new AttendanceMatrixPivotResponseDto
                    {
                        Success = false,
                        Message = "No hay datos para procesar",
                        Employees = new List<EmployeePivotData>(),
                        DateRange = new List<DateTime>(),
                        Summary = new AttendanceSummaryPivotDto()
                    };
                }

                // Procesar pivot
                var pivotData = ProcessDataForPivot(rawData.Data, filter.FechaInicio, filter.FechaFin);
                var dateRange = GenerateDateRange(filter.FechaInicio, filter.FechaFin);

                // Generar resumen
                var summary = new AttendanceSummaryPivotDto
                {
                    TotalEmployees = pivotData.Count,
                    TotalWorkingDays = pivotData.Sum(e => e.DailyData.Count(d => d.Value.EntradaReal != "FALTA" && string.IsNullOrEmpty(d.Value.TipoPermiso))),
                    TotalAbsences = pivotData.Sum(e => e.DailyData.Count(d => d.Value.EntradaReal == "FALTA")),
                    TotalPermissions = pivotData.Sum(e => e.DailyData.Count(d => !string.IsNullOrEmpty(d.Value.TipoPermiso))),
                    TotalHours = pivotData.Sum(e => e.TotalHoras),
                    TotalOvertimeHours = pivotData.Sum(e => e.HorasExtras)
                };

                return new AttendanceMatrixPivotResponseDto
                {
                    Success = true,
                    Message = $"Pivot procesado exitosamente. {pivotData.Count} empleados encontrados.",
                    Employees = pivotData,
                    DateRange = dateRange,
                    Summary = summary,
                    GeneratedAt = DateTime.Now,
                    ExecutionTime = rawData.ExecutionTime,
                    TotalPages=rawData.TotalPages,
                    PageSize=rawData.PageSize,
                    CurrentPage = rawData.CurrentPage,
                    TotalRecords = rawData.TotalRecords
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar pivot de matriz de asistencia");
                return new AttendanceMatrixPivotResponseDto
                {
                    Success = false,
                    Message = $"Error interno: {ex.Message}",
                    Employees = new List<EmployeePivotData>(),
                    DateRange = new List<DateTime>(),
                    Summary = new AttendanceSummaryPivotDto()
                };
            }
        }

        #region Cost Center Report Helpers

        /// <summary>
        /// Genera agrupaciones por semana para el reporte
        /// </summary>
        private List<WeekGroup> GenerateWeekGroups(List<DateTime> dateRange)
        {
            var weekGroups = new List<WeekGroup>();
            var currentWeek = new List<DateTime>();
            var weekNumber = 1;

            foreach (var date in dateRange)
            {
                currentWeek.Add(date);

                // Si es domingo o es el último día, cerrar la semana
                if (date.DayOfWeek == DayOfWeek.Sunday || date == dateRange.Last())
                {
                    weekGroups.Add(new WeekGroup
                    {
                        WeekNumber = weekNumber++,
                        Dates = new List<DateTime>(currentWeek)
                    });
                    currentWeek.Clear();
                }
            }

            return weekGroups;
        }

        /// <summary>
        /// Obtiene el valor de centro de costos o concepto para un día específico
        /// </summary>
        private string GetCostCenterValue(EmployeePivotData employee, DateTime date)
        {
            if (!employee.DailyData.TryGetValue(date.Date, out var dayData))
                return string.Empty;

            // 1. Si hay permiso, mostrar concepto
            if (!string.IsNullOrEmpty(dayData.TipoPermiso))
            {
                return MapConceptFromPermiso(dayData.TipoPermiso);
            }

            // 2. Si es falta, mostrar F
            if (dayData.EntradaReal == "FALTA" || dayData.SalidaReal == "FALTA")
            {
                return "F"; // Falta
            }

            // 3. Si trabajó normal, mostrar CcCodigo
            if (!string.IsNullOrEmpty(employee.CCCodigo))
            {
                return employee.CCCodigo;
            }

            return string.Empty;
        }

        /// <summary>
        /// Obtiene el valor para el reporte de marcaciones (cantidad de marcaciones o concepto)
        /// </summary>
        private string GetMarkingsValue(EmployeePivotData employee, DateTime date)
        {
            if (!employee.DailyData.TryGetValue(date.Date, out var dayData))
                return "";

            // 1. Si hay permiso, mostrar concepto
            if (!string.IsNullOrEmpty(dayData.TipoPermiso))
            {
                return MapConceptFromPermiso(dayData.TipoPermiso);
            }

            // 2. Verificar marcaciones del día
            if (string.IsNullOrEmpty(dayData.MarcacionesDelDia) || 
                dayData.MarcacionesDelDia == "SIN_MARCACIONES")
            {
                return "F"; // Falta sin justificar
            }

            // 3. Contar marcaciones (split por |)
            var marcaciones = dayData.MarcacionesDelDia
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Count();

            return marcaciones.ToString();
        }

        /// <summary>
        /// Calcula las horas trabajadas en un día basado en entrada y salida
        /// </summary>
        private decimal CalculateDailyWorkedHours(DailyAttendanceData dayData)
        {
            if (dayData == null || string.IsNullOrEmpty(dayData.EntradaReal) || string.IsNullOrEmpty(dayData.SalidaReal))
                return 0;

            // Si hay permiso, no contar horas trabajadas
            if (!string.IsNullOrEmpty(dayData.TipoPermiso))
                return 0;

            // Si es falta, no hay horas trabajadas
            if (dayData.EntradaReal == "FALTA" || dayData.SalidaReal == "FALTA" || 
                dayData.EntradaReal == "-" || dayData.SalidaReal == "-")
                return 0;

            // Parsear horas de entrada y salida
            var entrada = ParseTimeToDecimal(dayData.EntradaReal);
            var salida = ParseTimeToDecimal(dayData.SalidaReal);

            if (entrada == null || salida == null)
                return 0;

            // Calcular diferencia en horas
            decimal horasTrabiajadas = salida.Value - entrada.Value;

            // Si trabajó hasta el día siguiente (horario nocturno)
            if (horasTrabiajadas < 0)
            {
                horasTrabiajadas += 24; // Agregar 24 horas
            }

            // Validar que sea un rango razonable (entre 0 y 24 horas)
            if (horasTrabiajadas < 0 || horasTrabiajadas > 24)
                return 0;

            return Math.Round(horasTrabiajadas, 2);
        }

        /// <summary>
        /// Convierte string de tiempo (HH:mm) a decimal (ej: "08:30" -> 8.5)
        /// </summary>
        private decimal? ParseTimeToDecimal(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr) || timeStr == "FALTA" || timeStr == "-")
                return null;

            // Formato esperado: "08:00", "17:30", etc.
            var timeParts = timeStr.Split(':');
            if (timeParts.Length != 2)
                return null;

            if (int.TryParse(timeParts[0], out var hours) && int.TryParse(timeParts[1], out var minutes))
            {
                return hours + (minutes / 60m);
            }

            return null;
        }

        /// <summary>
        /// Mapea tipos de permiso a conceptos del reporte
        /// </summary>
        private string MapConceptFromPermiso(string tipoPermiso)
        {
            if (string.IsNullOrEmpty(tipoPermiso))
                return string.Empty;

            var conceptMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Mapeo de conceptos
                { "VACACIONES", "VA" },
                { "DESCANSO MEDICO", "DM" },
                { "SUBSIDIO", "SUB" },
                { "LICENCIA PATERNIDAD", "LP" },
                { "LICENCIA CON GOCE DE HABER", "LCGH" },
                { "PERMISO", "P" },
                { "FALTA JUSTIFICADA", "FJ" },
                { "FALTA NO JUSTIFICADA", "FI" },
                { "SUSPENSION", "S" },
                { "CESADO", "CESE" }
            };

            // Buscar mapeo exacto
            if (conceptMap.TryGetValue(tipoPermiso, out var concept))
                return concept;

            // Si no encuentra mapeo exacto, buscar por contiene
            foreach (var kvp in conceptMap)
            {
                if (tipoPermiso.ToUpper().Contains(kvp.Key.ToUpper()) || 
                    kvp.Key.ToUpper().Contains(tipoPermiso.ToUpper()))
                {
                    return kvp.Value;
                }
            }

            // Si no encuentra ningún mapeo, devolver las primeras letras
            return tipoPermiso.Length > 2 ? tipoPermiso.Substring(0, 2).ToUpper() : tipoPermiso.ToUpper();
        }

        /// <summary>
        /// Obtiene el nombre del día de la semana en español
        /// </summary>
        private string GetSpanishDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "LUNES",
                DayOfWeek.Tuesday => "MARTES",
                DayOfWeek.Wednesday => "MIÉRCOLES", 
                DayOfWeek.Thursday => "JUEVES",
                DayOfWeek.Friday => "VIERNES",
                DayOfWeek.Saturday => "SÁBADO",
                DayOfWeek.Sunday => "DOMINGO",
                _ => dayOfWeek.ToString().ToUpper()
            };
        }

        /// <summary>
        /// Obtiene el turno más común de una semana para un empleado
        /// </summary>
        private string GetWeekTurno(EmployeePivotData employee, List<DateTime> weekDates)
        {
            var turnos = new List<string>();

            foreach (var date in weekDates)
            {
                if (employee.DailyData.TryGetValue(date.Date, out var dayData) && 
                    !string.IsNullOrEmpty(dayData.TurnoNombre))
                {
                    turnos.Add(dayData.TurnoNombre);
                }
            }

            if (!turnos.Any())
                return "S/T"; // Sin Turno

            // Obtener el turno más común
            var turnoMasComun = turnos
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            // Abreviar turno si es muy largo
            return AbbreviateTurno(turnoMasComun);
        }

        /// <summary>
        /// Abrevia nombres de turno largos
        /// </summary>
        private string AbbreviateTurno(string turnoNombre)
        {
            if (string.IsNullOrEmpty(turnoNombre))
                return "S/T";

            var abreviaciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DIA", "DIA" },
                { "NOCHE", "NOCHE" },
                { "MAÑANA", "MAÑ" },
                { "TARDE", "TARDE" }
            };

            foreach (var kvp in abreviaciones)
            {
                if (turnoNombre.ToUpper().Contains(kvp.Key))
                    return kvp.Value;
            }

            // Si no encuentra abreviación, tomar primeras letras
            return turnoNombre.Length > 6 ? turnoNombre.Substring(0, 6).ToUpper() : turnoNombre.ToUpper();
        }

        /// <summary>
        /// Abrevia los nombres de planilla para mostrar en el Excel
        /// </summary>
        private string GetAbbreviatedPlanilla(string planilla)
        {
            if (string.IsNullOrEmpty(planilla))
                return "EMP"; // Default

            var planillaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "EMPLEADOS FIBRAFIL", "EMP-FIB" },
                { "OBREROS", "OBR" },
                { "4TA CATEGORIA", "4TA-CAT" },
                { "EMPLEADOS-FIBRAPRINT", "EMP-FPR" },
                { "FUNCIONARIOS", "FUNC" },
                { "EMPLEADOS", "EMP" },
                { "TERCEROS", "TERC" },
                { "PRACTICANTES", "PRAC" },
                { "CONSULTORES", "CONS" }
            };

            // Buscar coincidencia exacta primero
            if (planillaMap.ContainsKey(planilla))
            {
                return planillaMap[planilla];
            }

            // Buscar coincidencia parcial
            foreach (var kvp in planillaMap)
            {
                if (planilla.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            // Si no encuentra ningún match, crear abreviación automática
            // Tomar las primeras letras de cada palabra importante
            var words = planilla.Split(new char[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                return string.Join("-", words.Take(2).Select(w => w.Length > 3 ? w.Substring(0, 3).ToUpper() : w.ToUpper()));
            }

            // Si es una sola palabra, tomar las primeras 6 letras máximo
            return planilla.Length > 6 ? planilla.Substring(0, 6).ToUpper() : planilla.ToUpper();
        }

        /// <summary>
        /// Aplica formato específico al reporte de centro de costos
        /// </summary>
        private void ApplyCostCenterFormatting(IXLWorksheet worksheet, int totalRows, int totalCols)
        {
            // Ajustar anchos de columna - MÁS GRANDES para mejor visualización
            worksheet.Column(1).Width = 5;   // ITEM
            worksheet.Column(2).Width = 10;  // PLANILLA
            worksheet.Column(3).Width = 12;  // NRO DOC
            worksheet.Column(4).Width = 35;  // NOMBRES
            worksheet.Column(5).Width = 20;  // AREA
            worksheet.Column(6).Width = 25;  // CARGO
            worksheet.Column(7).Width = 12;  // FECHA INGRESO

            // Columnas de días y turno (más anchas para códigos de centro de costos)
            for (int i = 8; i <= totalCols; i++)
            {
                // Columnas de TURNO un poco más anchas
                if (IsGrayColumn(i, worksheet))
                {
                    worksheet.Column(i).Width = 8; // TURNO columnas
                }
                else
                {
                    worksheet.Column(i).Width = 12; // Días - más anchas para códigos de CC
                }
            }

            // Ajustar altura de filas para mejor visualización
            for (int row = 1; row <= totalRows; row++)
            {
                worksheet.Row(row).Height = 18; // Más altura para todas las filas
            }

            // Congelar filas y columnas
            worksheet.SheetView.FreezeRows(4); // Ahora son 4 filas de encabezado
            worksheet.SheetView.FreezeColumns(4);

            // Agregar bordes
            var dataRange = worksheet.Range(1, 1, totalRows, totalCols);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Alineación centrada para todas las celdas de datos
            for (int row = 5; row <= totalRows; row++) // Ahora empieza en la fila 5
            {
                for (int col = 8; col <= totalCols; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    
                    // Aplicar colores según el valor de la celda
                    var cellValue = cell.Value.ToString() ?? "";
                    ApplyCostCenterCellColor(cell, cellValue);
                }
            }
        }

        /// <summary>
        /// Determina si una columna es de TURNO (gris)
        /// </summary>
        private bool IsGrayColumn(int columnIndex, IXLWorksheet worksheet)
        {
            // Verificar si la celda en la fila de encabezado contiene "TURNO"
            var headerCell = worksheet.Cell(2, columnIndex);
            return headerCell.Value.ToString() == "TURNO";
        }

        /// <summary>
        /// Aplica colores a las celdas según los códigos de ausencias
        /// </summary>
        private void ApplyCostCenterCellColor(IXLCell cell, string cellValue)
        {
            if (string.IsNullOrWhiteSpace(cellValue) || cellValue == "-")
                return;

            // Colores según la tabla especificada
            switch (cellValue.ToUpper())
            {
                case "VA": // Vacaciones - Celeste/Turquesa
                    cell.Style.Fill.BackgroundColor = XLColor.LightCyan;
                    break;
                    
                case "DM": // Descanso Médico - Rosado/Rojo Claro
                    cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    break;
                    
                case "F": // Falta - Amarillo/Naranja
                    cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    break;
                    
                case "P": // Permiso - Un tono suave
                    cell.Style.Fill.BackgroundColor = XLColor.Lavender;
                    break;
                    
                default: // Día de Trabajo Normal (códigos de CC) - Sin color de fondo
                    // No aplicar color de fondo para códigos de centro de costos normales
                    break;
            }

            // Hacer el texto más visible con negrita para códigos especiales
            if (cellValue.Length <= 3 && (cellValue == "VA" || cellValue == "DM" || cellValue == "F" || cellValue == "P"))
            {
                cell.Style.Font.Bold = true;
            }
        }

        /// <summary>
        /// Aplica formato específico al reporte de marcaciones
        /// </summary>
        private void ApplyMarkingsFormatting(IXLWorksheet worksheet, int totalRows, int totalCols)
        {
            // Ajustar anchos de columna - Optimizado para números de marcaciones
            worksheet.Column(1).Width = 5;   // ITEM
            worksheet.Column(2).Width = 10;  // PLANILLA
            worksheet.Column(3).Width = 12;  // NRO DOC
            worksheet.Column(4).Width = 35;  // NOMBRES
            worksheet.Column(5).Width = 20;  // AREA
            worksheet.Column(6).Width = 25;  // CARGO
            worksheet.Column(7).Width = 12;  // FECHA INGRESO

            // Columnas de días y turno (suficiente para números 1-4)
            for (int i = 8; i <= totalCols; i++)
            {
                // Columnas de TURNO
                if (IsGrayColumn(i, worksheet))
                {
                    worksheet.Column(i).Width = 8; // TURNO columnas
                }
                else
                {
                    worksheet.Column(i).Width = 8; // Días - suficiente para números de marcaciones
                }
            }

            // Ajustar altura de filas para mejor visualización
            for (int row = 1; row <= totalRows; row++)
            {
                worksheet.Row(row).Height = 18;
            }

            // Congelar filas y columnas
            worksheet.SheetView.FreezeRows(4); // 4 filas de encabezado
            worksheet.SheetView.FreezeColumns(4);

            // Agregar bordes
            var dataRange = worksheet.Range(1, 1, totalRows, totalCols);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Alineación centrada para todas las celdas de datos
            for (int row = 5; row <= totalRows; row++) // Ahora empieza en la fila 5
            {
                for (int col = 8; col <= totalCols; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    
                    // Aplicar colores según el valor de la celda
                    var cellValue = cell.Value.ToString() ?? "";
                    ApplyMarkingsCellColor(cell, cellValue);
                }
            }
        }

        /// <summary>
        /// Aplica colores a las celdas del reporte de marcaciones
        /// </summary>
        private void ApplyMarkingsCellColor(IXLCell cell, string cellValue)
        {
            if (string.IsNullOrWhiteSpace(cellValue) || cellValue == "-")
                return;

            // Colores según la tabla especificada (mismos que Centro de Costos)
            switch (cellValue.ToUpper())
            {
                case "VA": // Vacaciones - Celeste/Turquesa
                    cell.Style.Fill.BackgroundColor = XLColor.LightCyan;
                    cell.Style.Font.Bold = true;
                    break;
                    
                case "DM": // Descanso Médico - Rosado/Rojo Claro
                    cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    cell.Style.Font.Bold = true;
                    break;
                    
                case "F": // Falta - Amarillo/Naranja
                    cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    cell.Style.Font.Bold = true;
                    break;
                    
                case "P": // Permiso - Un tono suave
                    cell.Style.Fill.BackgroundColor = XLColor.Lavender;
                    cell.Style.Font.Bold = true;
                    break;
                    
                default: 
                    // Para números de marcaciones (1, 2, 3, 4, etc.)
                    if (int.TryParse(cellValue, out var marcaciones))
                    {
                        // Colores progresivos según cantidad de marcaciones
                        switch (marcaciones)
                        {
                            case 1:
                                cell.Style.Fill.BackgroundColor = XLColor.LightGreen; // Verde claro - una marcación
                                break;
                            case 2: 
                                cell.Style.Fill.BackgroundColor = XLColor.White; // Blanco - dos marcaciones (normal)
                                break;
                            case 3:
                                cell.Style.Fill.BackgroundColor = XLColor.LightBlue; // Azul claro - tres marcaciones
                                break;
                            case 4:
                                cell.Style.Fill.BackgroundColor = XLColor.LightSalmon; // Salmón claro - cuatro marcaciones
                                break;
                            default:
                                if (marcaciones > 4)
                                {
                                    cell.Style.Fill.BackgroundColor = XLColor.Orange; // Naranja - muchas marcaciones
                                }
                                break;
                        }
                        
                        // Hacer negrita los números para mejor visibilidad
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontSize = 12;
                    }
                    break;
            }
        }

        /// <summary>
        /// Aplica formato específico al reporte de asistencia semanal
        /// </summary>
        private void ApplyWeeklyAttendanceFormatting(IXLWorksheet worksheet, int totalRows, int totalCols)
        {
            // Ajustar anchos de columna - Optimizado para entrada/salida y totales
            worksheet.Column(1).Width = 5;   // ITEM
            worksheet.Column(2).Width = 10;  // PLANILLA
            worksheet.Column(3).Width = 12;  // NRO DOC
            worksheet.Column(4).Width = 35;  // NOMBRES
            worksheet.Column(5).Width = 20;  // AREA
            worksheet.Column(6).Width = 15;  // CENTRO COSTO
            worksheet.Column(7).Width = 25;  // CARGO
            worksheet.Column(8).Width = 12;  // FECHA INGRESO

            // Columnas dinámicas (días y totales)
            for (int i = 9; i <= totalCols; i++)
            {
                worksheet.Column(i).Width = 10; // Ancho estándar para entrada/salida y totales
            }

            // Ajustar altura de filas para mejor visualización
            for (int row = 1; row <= totalRows; row++)
            {
                worksheet.Row(row).Height = 20; // Más altura para cabeceras complejas
            }

            // Congelar filas y columnas
            worksheet.SheetView.FreezeRows(5); // 5 filas de encabezado (título + 4 niveles)
            worksheet.SheetView.FreezeColumns(4);

            // Agregar bordes
            var dataRange = worksheet.Range(1, 1, totalRows, totalCols);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Alineación centrada para todas las celdas de datos
            for (int row = 6; row <= totalRows; row++) // Los datos empiezan en la fila 6
            {
                for (int col = 1; col <= totalCols; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    
                    // Alineación según el tipo de columna
                    if (col <= 8) // Columnas de información del empleado (ahora son 8)
                    {
                        if (col == 4) // Nombre del empleado
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        else
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else // Columnas de datos y totales
                    {
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }

            // Formato especial para columnas de totales semanales (verde claro)
            // y totales globales (verde oscuro) - ya aplicado en la generación

            _logger.LogInformation("Formato aplicado al reporte semanal con {TotalRows} filas y {TotalCols} columnas", 
                totalRows, totalCols);
        }

        /// <summary>
        /// Aplica colores a las celdas del reporte semanal según el estado de asistencia
        /// </summary>
        private void ApplyWeeklyAttendanceCellColor(IXLCell cell, DailyAttendanceData dayData, string tipo)
        {
            var cellValue = cell.Value.ToString() ?? "";

            // 1. Si hay permiso/justificación - COLOR AZUL
            if (!string.IsNullOrEmpty(dayData.TipoPermiso))
            {
                cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                cell.Style.Font.Bold = true;
                
                // NO cambiar el valor de la celda, solo aplicar color azul
                return;
            }

            // 2. Si es falta sin justificar - COLOR ROJO
            if (cellValue == "FALTA" || cellValue == "-" || string.IsNullOrEmpty(cellValue))
            {
                cell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                
                if (cellValue == "FALTA")
                {
                    cell.Style.Font.Bold = true;
                }
                return;
            }

            // 3. Si tiene hora válida - COLOR VERDE CLARO (trabajo normal)
            if (!string.IsNullOrEmpty(cellValue) && cellValue != "FALTA" && cellValue != "-")
            {
                // Verificar si es una hora válida (formato HH:mm)
                if (IsValidTime(cellValue))
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                }
                return;
            }

            // 4. Por defecto - sin color especial
        }

        /// <summary>
        /// Verifica si un string tiene formato de hora válido (HH:mm)
        /// </summary>
        private bool IsValidTime(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr))
                return false;

            // Formato esperado: "08:00", "17:30", etc.
            var timeParts = timeStr.Split(':');
            if (timeParts.Length != 2)
                return false;

            return int.TryParse(timeParts[0], out var hours) && 
                   int.TryParse(timeParts[1], out var minutes) &&
                   hours >= 0 && hours <= 23 &&
                   minutes >= 0 && minutes <= 59;
        }

        #endregion

        /// <summary>
        /// Clase helper para agrupación por semanas
        /// </summary>
        private class WeekGroup
        {
            public int WeekNumber { get; set; }
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
        }
    }
}
