using Dtos.OHLD;
using Entities.OHLD;
using FibAttendanceApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FibAttendanceApi.Core.OhldService
{
    public class OhldSyncService
    {
        private readonly ApplicationDbcontext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OhldSyncService> _logger;
        private readonly string _externalApiUrl = "http://192.168.1.13:9060/api/Ohlds";

        public OhldSyncService(
            ApplicationDbcontext context,
            HttpClient httpClient,
            ILogger<OhldSyncService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza los datos de OHLD desde la API externa
        /// </summary>
        /// <param name="useFullReplacement">Si es true, limpia toda la tabla antes de insertar. Si es false, hace comparación incremental</param>
        /// <returns>Resultado de la sincronización</returns>
        public async Task<SynchronizationResultDto> SynchronizeOhldDataAsync(bool useFullReplacement = true)
        {
            var result = new SynchronizationResultDto
            {
                SynchronizedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Iniciando sincronización de datos OHLD desde API externa");

                // 1. Obtener datos de la API externa
                var externalData = await FetchExternalDataAsync();
                if (externalData == null || !externalData.Any())
                {
                    result.Success = false;
                    result.Message = "No se pudieron obtener datos de la API externa";
                    return result;
                }

                result.TotalRecordsProcessed = externalData.Count;
                _logger.LogInformation($"Se obtuvieron {externalData.Count} registros de la API externa");

                // 2. Ejecutar sincronización según estrategia
                if (useFullReplacement)
                {
                    await ExecuteFullReplacementSync(externalData, result);
                }
                else
                {
                    await ExecuteIncrementalSync(externalData, result);
                }

                result.Success = true;
                result.Message = $"Sincronización completada exitosamente. Registros procesados: {result.TotalRecordsProcessed}";
                
                _logger.LogInformation("Sincronización completada exitosamente");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error durante la sincronización: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "Error durante la sincronización de datos OHLD");
            }

            return result;
        }

        /// <summary>
        /// Obtiene los datos desde la API externa
        /// </summary>
        private async Task<List<ExternalOhldDto>?> FetchExternalDataAsync()
        {
            try
            {
                _logger.LogInformation($"Realizando petición GET a: {_externalApiUrl}");
                
                var response = await _httpClient.GetAsync(_externalApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error en petición HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogWarning("La respuesta de la API externa está vacía");
                    return new List<ExternalOhldDto>();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var externalData = JsonSerializer.Deserialize<List<ExternalOhldDto>>(jsonContent, options);
                
                _logger.LogInformation($"Datos deserializados correctamente. Total registros: {externalData?.Count ?? 0}");
                
                return externalData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de la API externa");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta sincronización completa (limpia tabla y reinserta todo)
        /// </summary>
        private async Task ExecuteFullReplacementSync(List<ExternalOhldDto> externalData, SynchronizationResultDto result)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation("Ejecutando sincronización completa (reemplazo total)");

                // 1. Limpiar tablas existentes
                _logger.LogInformation("Eliminando registros existentes...");
                
                var existingHld1s = await _context.Hld1s.ToListAsync();
                var existingOhlds = await _context.Ohlds.ToListAsync();
                
                result.RecordsDeleted = existingHld1s.Count + existingOhlds.Count;
                
                _context.Hld1s.RemoveRange(existingHld1s);
                _context.Ohlds.RemoveRange(existingOhlds);
                
                await _context.SaveChangesAsync();

                // 2. Insertar nuevos datos
                await InsertNewRecords(externalData, result);

                await transaction.CommitAsync();
                _logger.LogInformation("Sincronización completa finalizada exitosamente");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error durante sincronización completa, ejecutando rollback");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta sincronización incremental (compara y actualiza solo diferencias)
        /// </summary>
        private async Task ExecuteIncrementalSync(List<ExternalOhldDto> externalData, SynchronizationResultDto result)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation("Ejecutando sincronización incremental");

                // 1. Obtener registros existentes
                var existingOhlds = await _context.Ohlds
                    .Include(o => o.Hld1s)
                    .ToDictionaryAsync(o => o.HldCode);

                var externalHldCodes = externalData.Select(e => e.HldCode).ToHashSet();

                // 2. Eliminar registros que ya no existen en API externa
                var toDelete = existingOhlds.Where(kvp => !externalHldCodes.Contains(kvp.Key)).ToList();
                foreach (var item in toDelete)
                {
                    _context.Ohlds.Remove(item.Value);
                    result.RecordsDeleted++;
                }

                // 3. Procesar cada registro de la API externa
                foreach (var externalOhld in externalData)
                {
                    if (existingOhlds.TryGetValue(externalOhld.HldCode, out var existingOhld))
                    {
                        // Actualizar registro existente si hay diferencias
                        if (HasChanges(existingOhld, externalOhld))
                        {
                            UpdateExistingRecord(existingOhld, externalOhld);
                            result.ExistingRecordsUpdated++;
                        }
                    }
                    else
                    {
                        // Crear nuevo registro
                        var newOhld = MapToEntity(externalOhld);
                        _context.Ohlds.Add(newOhld);
                        result.NewRecordsAdded++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Sincronización incremental finalizada exitosamente");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error durante sincronización incremental, ejecutando rollback");
                throw;
            }
        }

        /// <summary>
        /// Inserta nuevos registros en la base de datos
        /// </summary>
        private async Task InsertNewRecords(List<ExternalOhldDto> externalData, SynchronizationResultDto result)
        {
            _logger.LogInformation("Insertando nuevos registros...");
            
            foreach (var externalOhld in externalData)
            {
                try
                {
                    var newOhld = MapToEntity(externalOhld);
                    _context.Ohlds.Add(newOhld);
                    result.NewRecordsAdded++;
                }
                catch (Exception ex)
                {
                    var error = $"Error al procesar registro {externalOhld.HldCode}: {ex.Message}";
                    result.Errors.Add(error);
                    _logger.LogWarning(error);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Se insertaron {result.NewRecordsAdded} nuevos registros");
        }

        /// <summary>
        /// Verifica si un registro existente tiene cambios comparado con el externo
        /// </summary>
        private bool HasChanges(Ohld existing, ExternalOhldDto external)
        {
            // Comparar propiedades principales
            if (existing.WndFrm != external.WndFrm ||
                existing.WndTo != external.WndTo ||
                existing.IsCurYear != external.IsCurYear ||
                existing.IgnrWnd != external.IgnrWnd ||
                existing.WeekNoRule != external.WeekNoRule)
            {
                return true;
            }

            // Comparar Hld1s
            if (existing.Hld1s.Count != external.Hld1s.Count)
            {
                return true;
            }

            var existingHld1s = existing.Hld1s.OrderBy(h => h.StrDate).ToList();
            var externalHld1s = external.Hld1s.OrderBy(h => h.StrDate).ToList();

            for (int i = 0; i < existingHld1s.Count; i++)
            {
                if (existingHld1s[i].StrDate != externalHld1s[i].StrDate ||
                    existingHld1s[i].EndDate != externalHld1s[i].EndDate ||
                    existingHld1s[i].Rmrks != externalHld1s[i].Rmrks)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Actualiza un registro existente con datos externos
        /// </summary>
        private void UpdateExistingRecord(Ohld existing, ExternalOhldDto external)
        {
            // Actualizar propiedades principales
            existing.WndFrm = external.WndFrm;
            existing.WndTo = external.WndTo;
            existing.IsCurYear = external.IsCurYear;
            existing.IgnrWnd = external.IgnrWnd;
            existing.WeekNoRule = external.WeekNoRule;

            // Limpiar Hld1s existentes y agregar nuevos
            existing.Hld1s.Clear();
            
            foreach (var externalHld1 in external.Hld1s)
            {
                existing.Hld1s.Add(new Hld1
                {
                    HldCode = externalHld1.HldCode,
                    StrDate = externalHld1.StrDate,
                    EndDate = externalHld1.EndDate,
                    Rmrks = externalHld1.Rmrks
                });
            }
        }

        /// <summary>
        /// Mapea un DTO externo a entidad de base de datos
        /// </summary>
        private Ohld MapToEntity(ExternalOhldDto external)
        {
            var ohld = new Ohld
            {
                HldCode = external.HldCode,
                WndFrm = external.WndFrm,
                WndTo = external.WndTo,
                IsCurYear = external.IsCurYear,
                IgnrWnd = external.IgnrWnd,
                WeekNoRule = external.WeekNoRule,
                Hld1s = external.Hld1s.Select(h => new Hld1
                {
                    HldCode = h.HldCode,
                    StrDate = h.StrDate,
                    EndDate = h.EndDate,
                    Rmrks = h.Rmrks
                }).ToList()
            };

            return ohld;
        }
    }
}