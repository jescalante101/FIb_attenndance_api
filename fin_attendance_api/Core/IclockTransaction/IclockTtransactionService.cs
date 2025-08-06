// Core/IclockTransaction/IclockTransactionService.cs
using Dtos.Transactions;
using FibAttendanceApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FibAttendanceApi.Core.IclockTransaction
{
    public class IclockTtransactionService : IIclockTransactionService
    {
        private readonly string _connectionString;

        public IclockTtransactionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // ===== MÉTODOS ORIGINALES (mantener compatibilidad) =====

        /// <summary>
        /// Método original sin paginación (para mantener compatibilidad)
        /// </summary>
        public async Task<List<IclockTransactionDto>> GetAsistenciasCompletasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var resultados = await connection.QueryAsync<IclockTransactionDtoWithTotal>(
                    "sp_GetAsistenciasCompletas",
                    new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        EmpleadoFilter = empleadoFilter,
                        PageNumber = 1,
                        PageSize = int.MaxValue // Sin límite para obtener todos
                    },
                    commandType: CommandType.StoredProcedure
                );

                // Mapear a tu DTO original (sin TotalRecords)
                return resultados.Select(r => new IclockTransactionDto
                {
                    NroDoc = r.Nro_Doc ?? string.Empty,
                    Nombres = r.Nombres ?? string.Empty,
                    ApellidoPaterno = r.Apellido_Paterno ?? string.Empty,
                    ApellidoMaterno = r.Apellido_Materno ?? string.Empty,
                    AreaDescripcion = r.AreaDescripcion ?? "Sin área",
                    CcostoDescripcion = r.CcostoDescripcion ?? "Sin centro de costo",
                    TurnoAlias = r.TurnoAlias ?? "Sin turno",
                    TerminalAlias = r.TerminalAlias ?? "N/A",
                    PunchTime = r.PunchTime,
                    HorarioAlias = r.HorarioAlias ?? "Sin horario",
                    InTime = r.InTime,
                    OutTime = r.OutTime,
                    DiaSemana = r.DiaSemana
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al obtener asistencias: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Método original con paginación
        /// </summary>
        public async Task<PagedResult<IclockTransactionDto>> GetAsistenciasCompletasPaginadasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var resultados = await connection.QueryAsync<IclockTransactionDtoWithTotal>(
                    "sp_GetAsistenciasCompletas",
                    new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        EmpleadoFilter = empleadoFilter,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    },
                    commandType: CommandType.StoredProcedure
                );

                var data = resultados.Select(r => new IclockTransactionDto
                {
                    NroDoc = r.Nro_Doc ?? string.Empty,
                    Nombres = r.Nombres ?? string.Empty,
                    ApellidoPaterno = r.Apellido_Paterno ?? string.Empty,
                    ApellidoMaterno = r.Apellido_Materno ?? string.Empty,
                    AreaDescripcion = r.AreaDescripcion ?? "Sin área",
                    CcostoDescripcion = r.CcostoDescripcion ?? "Sin centro de costo",
                    TurnoAlias = r.TurnoAlias ?? "Sin turno",
                    TerminalAlias = r.TerminalAlias ?? "N/A",
                    PunchTime = r.PunchTime,
                    HorarioAlias = r.HorarioAlias ?? "Sin horario",
                    InTime = r.InTime,
                    OutTime = r.OutTime,
                    DiaSemana = r.DiaSemana
                }).ToList();

                // Obtener información de paginación del primer registro
                var totalRecords = resultados.FirstOrDefault()?.TotalRecords ?? 0;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new PagedResult<IclockTransactionDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al obtener asistencias paginadas: {ex.Message}", ex);
            }
        }

        // ===== MÉTODOS NUEVOS CON ANÁLISIS MEJORADO =====

        /// <summary>
        /// NUEVO - Obtiene análisis completo de asistencias sin paginación
        /// Usa el SP mejorado con lógica de proximidad temporal
        /// </summary>
        public async Task<List<AsistenciaCompletaDto>> GetAnalisisAsistenciasCompletasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var resultados = await connection.QueryAsync<AsistenciaCompletaDtoWithTotal>(
                    "sp_GetAsistenciasCompletas2", // El SP mejorado
                    new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        EmpleadoFilter = empleadoFilter,
                        PageNumber = 1,
                        PageSize = int.MaxValue // Sin límite para obtener todos
                    },
                    commandType: CommandType.StoredProcedure
                );

                // Mapear directamente al DTO completo
                return resultados.Select(MapToAsistenciaCompleta).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al obtener análisis de asistencias: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// NUEVO - Obtiene análisis completo de asistencias con paginación
        /// Usa el SP mejorado con lógica de proximidad temporal
        /// </summary>
        public async Task<PagedResult<AsistenciaCompletaDto>> GetAnalisisAsistenciasCompletasPaginadasAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            string empleadoFilter = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var resultados = await connection.QueryAsync<AsistenciaCompletaDtoWithTotal>(
                    "sp_GetAsistenciasCompletas2", // El SP mejorado
                    new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,
                        EmpleadoFilter = empleadoFilter,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    },
                    commandType: CommandType.StoredProcedure
                );

                var data = resultados.Select(MapToAsistenciaCompleta).ToList();

                // Obtener información de paginación del primer registro
                var totalRecords = resultados.FirstOrDefault()?.TotalRecords ?? 0;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new PagedResult<AsistenciaCompletaDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al obtener análisis de asistencias paginadas: {ex.Message}", ex);
            }
        }

        // ===== MÉTODOS PRIVADOS DE MAPEO =====

        /// <summary>
        /// Mapea los resultados del SP al DTO completo
        /// </summary>
        private static AsistenciaCompletaDto MapToAsistenciaCompleta(AsistenciaCompletaDtoWithTotal result)
        {
            return new AsistenciaCompletaDto
            {
                // Información del empleado
                Nro_Doc = result.Nro_Doc ?? string.Empty,
                Nombres = result.Nombres ?? string.Empty,
                Apellido_Paterno = result.Apellido_Paterno ?? string.Empty,
                Apellido_Materno = result.Apellido_Materno ?? string.Empty,
                AreaDescripcion = result.AreaDescripcion ?? "Sin área",
                CcostoDescripcion = result.CcostoDescripcion ?? "Sin centro de costo",

                // Información de turno y horario
                TurnoAlias = result.TurnoAlias ?? "Sin turno",
                HorarioAlias = result.HorarioAlias ?? "Sin horario",

                // Fecha y horarios
                FechaMarcacion = result.FechaMarcacion,
                HorarioEntrada = result.HorarioEntrada,
                HorarioSalida = result.HorarioSalida,

                // Marcaciones reales
                HoraEntrada = result.HoraEntrada,
                HoraSalida = result.HoraSalida,
                TerminalEntrada = result.TerminalEntrada,
                TerminalSalida = result.TerminalSalida,

                // Análisis de marcaciones
                TotalMarcacionesDia = result.TotalMarcacionesDia,
                MarcacionesEsperadas = result.MarcacionesEsperadas,
                ContadorEntradas = result.ContadorEntradas,
                ContadorSalidas = result.ContadorSalidas,
                ContadorBreaks = result.ContadorBreaks,

                // Estados y análisis
                EstadoEntrada = result.EstadoEntrada ?? string.Empty,
                EstadoDia = result.EstadoDia ?? string.Empty,
                EstadoMarcaciones = result.EstadoMarcaciones ?? string.Empty,

                // Cálculos de tiempo
                MinutosTardanza = result.MinutosTardanza,
                MinutosSalidaTemprana = result.MinutosSalidaTemprana,
                MinutosTrabajados = result.MinutosTrabajados,

                // Información adicional
                DiaSemana = result.DiaSemana,
                DetalleMarcaciones = result.DetalleMarcaciones
            };
        }
    }
}