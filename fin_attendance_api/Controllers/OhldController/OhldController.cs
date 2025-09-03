using Entities.OHLD;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FibAttendanceApi.Core.OhldService;
using Dtos.OHLD;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FibAttendanceApi.Controllers.OhldController
{

    [Route("api/[controller]")]
    [ApiController]
    public class OhldsController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly OhldSyncService _syncService;

        public OhldsController(ApplicationDbcontext context, OhldSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        // GET: api/Ohlds
        // Obtiene todos los registros de OHLD
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ohld>>> GetOhlds()
        {
            if (_context.Ohlds == null)
            {
                return NotFound("La entidad 'Ohlds' no está configurada en el DbContext.");
            }
            var ohlds = await _context.Ohlds
                .Include(o => o.Hld1s) // Incluye los registros relacionados de Hld1
                .ToListAsync();
            
            var ohldsWithHld1s = ohlds.Select(o => new
            {
                o.HldCode,
                o.WndFrm,
                o.WndTo,
                o.IsCurYear,
                o.IgnrWnd,
                o.WeekNoRule,
                Hld1s = o.Hld1s.Select(h => new
                {
                    h.StrDate,
                    h.EndDate,
                    h.Rmrks, // Asumiendo que Hld1 tiene una propiedad Description
                    h.HldCode
                }).ToList()
            });


            return Ok(ohldsWithHld1s);
        }

        // GET: api/Ohlds/5
        // Obtiene un registro específico de OHLD por su HldCode
        [HttpGet("{id}")]
        public async Task<ActionResult<Ohld>> GetOhld(string id)
        {
            if (_context.Ohlds == null)
            {
                return NotFound();
            }
            var ohld =  await _context.Ohlds.
                Include(o => o.Hld1s)
               .ToListAsync();

           


            var ohldsWithHld1s = ohld
               .Where(x => x.HldCode == id)
                .Select(o => new
            {
                o.HldCode,
                o.WndFrm,
                o.WndTo,
                o.IsCurYear,
                o.IgnrWnd,
                o.WeekNoRule,
                Hld1s = o.Hld1s.Select(h => new
                {
                    h.StrDate,
                    h.EndDate,
                    h.Rmrks, // Asumiendo que Hld1 tiene una propiedad Description
                    h.HldCode
                })
                .ToList()
            }).FirstOrDefault();


            if (ohldsWithHld1s == null)
            {
                return NotFound($"No se encontró ningún registro con el HldCode: {id}");
            }

            return  Ok(ohldsWithHld1s);
        }

        // PUT: api/Ohlds/5
        // Actualiza un registro existente.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOhld(string id, Ohld ohld)
        {
            if (id != ohld.HldCode)
            {
                return BadRequest("El 'HldCode' del URL no coincide con el del cuerpo de la solicitud.");
            }

            _context.Entry(ohld).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OhldExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Respuesta estándar para una actualización exitosa
        }

        // POST: api/Ohlds
        // Crea un nuevo registro en OHLD
        [HttpPost]
        public async Task<ActionResult<Ohld>> PostOhld(Ohld ohld)
        {
            if (_context.Ohlds == null)
            {
                return Problem("La entidad 'Ohlds' no está configurada en el DbContext.");
            }

            // Opcional: Verificar si ya existe para evitar errores de clave primaria
            if (OhldExists(ohld.HldCode))
            {
                return Conflict($"Ya existe un registro con el HldCode: {ohld.HldCode}");
            }

            _context.Ohlds.Add(ohld);
            await _context.SaveChangesAsync();

            // Retorna una respuesta 201 Created con la ubicación del nuevo recurso
            return CreatedAtAction("GetOhld", new { id = ohld.HldCode }, ohld);
        }

        // DELETE: api/Ohlds/5
        // Elimina un registro de OHLD
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOhld(string id)
        {
            if (_context.Ohlds == null)
            {
                return NotFound();
            }
            var ohld = await _context.Ohlds.FindAsync(id);
            if (ohld == null)
            {
                return NotFound();
            }

            _context.Ohlds.Remove(ohld);
            await _context.SaveChangesAsync();

            return NoContent(); // Respuesta estándar para una eliminación exitosa
        }

        // POST: api/Ohlds/synchronize
        // Sincroniza datos desde API externa
        [HttpPost("synchronize")]
        public async Task<ActionResult<SynchronizationResultDto>> SynchronizeOhldData([FromQuery] bool useFullReplacement = true)
        {
            try
            {
                var result = await _syncService.SynchronizeOhldDataAsync(useFullReplacement);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SynchronizationResultDto
                {
                    Success = false,
                    Message = $"Error interno del servidor: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    SynchronizedAt = DateTime.UtcNow
                });
            }
        }

        // POST: api/Ohlds/bulk
        // Inserta múltiples registros OHLD de una vez
        [HttpPost("bulk")]
        public async Task<ActionResult<string>> PostBulkOhlds([FromBody] List<Ohld> ohlds)
        {
            if (_context.Ohlds == null)
            {
                return Problem("La entidad 'Ohlds' no está configurada en el DbContext.");
            }

            if (ohlds == null || !ohlds.Any())
            {
                return BadRequest("La lista de OHLDs no puede estar vacía.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                int inserted = 0;
                int updated = 0;
                List<string> errors = new List<string>();

                foreach (var ohld in ohlds)
                {
                    try
                    {
                        if (OhldExists(ohld.HldCode))
                        {
                            _context.Entry(ohld).State = EntityState.Modified;
                            updated++;
                        }
                        else
                        {
                            _context.Ohlds.Add(ohld);
                            inserted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error procesando HldCode {ohld.HldCode}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = $"Operación completada. Insertados: {inserted}, Actualizados: {updated}";
                if (errors.Any())
                {
                    message += $", Errores: {errors.Count}";
                }

                return Ok(new
                {
                    Message = message,
                    Inserted = inserted,
                    Updated = updated,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error durante la operación bulk: {ex.Message}");
            }
        }

        // Método privado para verificar si un registro existe
        private bool OhldExists(string id)
        {
            return (_context.Ohlds?.Any(e => e.HldCode == id)).GetValueOrDefault();
        }
    }

}
