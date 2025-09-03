using ClosedXML.Excel;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Dtos.Reportes.HorasExtras;
using FibAttendanceApi.Data;
using Microsoft.Data.SqlClient; // Necesario para FromSqlRaw

public class ExcelReportService
{
    private readonly ApplicationDbcontext _context;

    public ExcelReportService(ApplicationDbcontext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerarReporteAsistencia(ReportFiltersHE filters)
    {
        // =======================================================
        // PASO 1: Ejecutar el SP y obtener los datos "planos"
        // =======================================================
        var sql = "EXEC [dbo].[sp_AttendanceCalculated_Final] @FechaInicio, @FechaFin, @CompaniaId, @EmployeeIds, @AreaId, @SedeId";
        var parameters = new[]
    {
            new SqlParameter("@FechaInicio", filters.StartDate),
        new SqlParameter("@FechaFin", filters.EndDate),
        new SqlParameter("@CompaniaId", filters.CompanyId),

            // Para los parámetros opcionales, si son nulos, enviamos DBNull.Value
        new SqlParameter("@EmployeeIds", (object)filters.EmployeeIds ?? DBNull.Value),
            new SqlParameter("@AreaId", (object)filters.AreaId ?? DBNull.Value),
        new SqlParameter("@SedeId", (object)filters.SedeId ?? DBNull.Value)
    };
        var datosPlanos = await _context.Set<AsistenciaDiariaSpDto>()
            .FromSqlRaw(sql, parameters)
            .ToListAsync();

        // =======================================================
        // PASO 2: Pivotear los datos usando LINQ
        // =======================================================
        var datosPivoteados = datosPlanos
            .GroupBy(d => new { d.Nro_Doc, d.Colaborador })
            .Select(g => {
                var firstRecord = g.First();
                var reporteEmpleado = new ReporteAsistenciaSemanalDto
                {
                    Nro_Doc = g.Key.Nro_Doc,
                    Colaborador = g.Key.Colaborador,
                    Area = firstRecord.Area,
                    Sede = firstRecord.Sede,
                    Cargo = firstRecord.Cargo,
                    FechaIngreso = firstRecord.Fecha
                };

                foreach (var dia in g)
                {
                    var asistenciaDia = new AsistenciaDiaReporteDto
                    {
                        HoraEntrada = dia.HoraEntrada,
                        HoraSalida = dia.HoraSalida,
                        // Convertimos minutos a horas
                        HorasNormales = Math.Round(dia.HorasNormales / 60.0, 2),
                        HorasExtras1 = Math.Round(dia.HorasExtrasNivel1 / 60.0, 2),
                        HorasExtras2 = Math.Round(dia.HorasExtrasNivel2 / 60.0, 2),
                        Estado = dia.HoraEntrada // Para detectar FALTAS, VACACIONES, etc.
                    };
                    reporteEmpleado.AsistenciaPorDia[dia.Fecha.Date] = asistenciaDia;
                }

                // Calcular totales
                reporteEmpleado.TotalHorasNormales = reporteEmpleado.AsistenciaPorDia.Values.Sum(v => v.HorasNormales);
                reporteEmpleado.TotalHorasExtras1 = reporteEmpleado.AsistenciaPorDia.Values.Sum(v => v.HorasExtras1);
                reporteEmpleado.TotalHorasExtras2 = reporteEmpleado.AsistenciaPorDia.Values.Sum(v => v.HorasExtras2);

                return reporteEmpleado;
            })
            .ToList();

        // =======================================================
        // PASO 3: Generar el archivo Excel con ClosedXML
        // =======================================================
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Asistencia");

            // --- Crear Cabeceras ---
            worksheet.Cell("A1").Value = "REPORTE DE ASISTENCIA SEMANAL";
            worksheet.Range("A1:Z2").Merge().Style.Font.Bold = true;
            // ... (aquí iría toda la lógica para crear las cabeceras complejas como en la imagen)

            int filaActual = 5; // Empezar a escribir los datos en la fila 5

            foreach (var empleado in datosPivoteados)
            {
                worksheet.Cell(filaActual, 1).Value = empleado.Nro_Doc;
                worksheet.Cell(filaActual, 2).Value = empleado.Colaborador;

                int columnaActual = 3;
                // Loop por cada día en el rango de fechas
                var fechaInicio = filters.StartDate.Date;
                var fechaFin = filters.EndDate.Date;
                
                for (var fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
                {
                    if (empleado.AsistenciaPorDia.TryGetValue(fecha, out var asistenciaDia))
                    {
                        worksheet.Cell(filaActual, columnaActual++).Value = asistenciaDia.HoraEntrada;
                        worksheet.Cell(filaActual, columnaActual++).Value = asistenciaDia.HoraSalida;
                        worksheet.Cell(filaActual, columnaActual++).Value = asistenciaDia.HorasNormales;
                        worksheet.Cell(filaActual, columnaActual++).Value = asistenciaDia.HorasExtras1;
                        worksheet.Cell(filaActual, columnaActual++).Value = asistenciaDia.HorasExtras2;
                    }
                    else
                    {
                        columnaActual += 5; // Saltar las 5 columnas si no hay datos para ese día
                    }
                }

                // Escribir los totales
                worksheet.Cell(filaActual, columnaActual).Value = empleado.TotalHorasNormales;

                filaActual++;
            }

            // --- Guardar en memoria y devolver como byte array ---
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    
    }
}