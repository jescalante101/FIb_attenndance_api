using AutoMapper;
using Dtos.TimeIntervalDto;
using Entities.Shifts;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Controllers.ShiftController
{
    [ApiController]
    [Route("api/TimeIntervals")] // Ruta más limpia y estándar (RESTful)
    public class AttTimeIntervalController : ControllerBase // Hereda de ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public AttTimeIntervalController(ApplicationDbcontext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // --- ENDPOINTS NUEVOS Y LIMPIOS ---

        /// <summary>
        /// Obtiene una lista paginada de horarios POR COMPAÑÍA, incluyendo los detalles de sus descansos.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeIntervalDetailDto>>> GetAll(
            [FromQuery] string companyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15)
        {
            if (string.IsNullOrEmpty(companyId))
            {
                return BadRequest("El parámetro 'companyId' es obligatorio.");
            }

            var query = _context.AttTimeintervals
                .Where(h => h.CompaniaId == companyId);

            var totalRecords = await query.CountAsync();

            var timeIntervals = await _context.AttTimeintervals
               .Where(h => h.CompaniaId == companyId)
               .Include(t => t.AttTimeintervalBreakTimes)
                   .ThenInclude(tb => tb.Breaktime)
               .OrderByDescending(h => h.Id)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            var dtos = _mapper.Map<List<TimeIntervalDetailDto>>(timeIntervals); // <-- Cambio aquí

            return Ok(new { data = dtos, totalRecords = totalRecords });
        }


        /// <summary>
        /// Obtiene un horario específico por su ID, incluyendo los detalles de sus descansos.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AttTimeIntervalDto>> GetById(int id)
        {
            var timeInterval = await _context.AttTimeintervals
             .Include(t => t.AttTimeintervalBreakTimes)
                 .ThenInclude(tb => tb.Breaktime)
             .FirstOrDefaultAsync(t => t.Id == id);
            // --- FIN DEL CAMBIO ---

            if (timeInterval == null)
            {
                return NotFound();
            }

            var dto = _mapper.Map<AttTimeIntervalDto>(timeInterval);
            return Ok(dto);
        }


        /// <summary>
        /// Crea un nuevo horario y asocia sus descansos.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AttTimeIntervalDto>> Create([FromBody] AttTimeIntervalCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newTimeInterval = _mapper.Map<AttTimeinterval>(createDto);


            // --- LÓGICA PARA CALCULAR Y ASIGNAR MÁRGENES ---
            // Hora de salida programada para el cálculo
            var scheduledOutTime = newTimeInterval.InTime.AddMinutes(newTimeInterval.Duration);

            // Asumimos que tienes una función que convierte "HH:mm" a un objeto de tiempo
            var punchInStart = TimeSpan.Parse(createDto.PunchInStartTime);
            var punchInEnd = TimeSpan.Parse(createDto.PunchInEndTime);
            var punchOutStart = TimeSpan.Parse(createDto.PunchOutStartTime);
            var punchOutEnd = TimeSpan.Parse(createDto.PunchOutEndTime);

            // Calculamos la diferencia en minutos
            newTimeInterval.InAheadMargin = (int)(newTimeInterval.InTime.TimeOfDay - punchInStart).TotalMinutes;
            newTimeInterval.InAboveMargin = (int)(punchInEnd - newTimeInterval.InTime.TimeOfDay).TotalMinutes;
            newTimeInterval.OutAheadMargin = (int)(scheduledOutTime.TimeOfDay - punchOutStart).TotalMinutes;
            newTimeInterval.OutAboveMargin = (int)(punchOutEnd - scheduledOutTime.TimeOfDay).TotalMinutes;

            // Asigna valores de auditoría
            newTimeInterval.CreatedAt = DateTime.Now;
            newTimeInterval.CreatedBy = User.Identity?.Name ?? "Sistema";
            newTimeInterval.DayChange = DateTime.Today;

            // --- NUEVA LÓGICA PARA ASOCIAR DESCANSOS ---
            if (createDto.BreakTimeIds != null && createDto.BreakTimeIds.Any())
            {
                foreach (var breakId in createDto.BreakTimeIds)
                {
                    // Crea la entidad de unión
                    var timeIntervalBreak = new AttTimeintervalBreakTime
                    {
                        BreaktimeId = breakId
                        // EF Core asignará el TimeintervalId automáticamente al añadirlo a la colección
                    };
                    newTimeInterval.AttTimeintervalBreakTimes.Add(timeIntervalBreak);
                }
            }
            // --- FIN DE LA NUEVA LÓGICA ---

            _context.AttTimeintervals.Add(newTimeInterval);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict($"Error al guardar: {ex.InnerException?.Message}");
            }

            var resultDto = _mapper.Map<AttTimeIntervalDto>(newTimeInterval);
            return CreatedAtAction(nameof(GetById), new { id = newTimeInterval.Id }, resultDto);
        }

        /// <summary>
        /// Actualiza un horario existente, sus descansos y márgenes de marcación.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AttTimeIntervalUpdateDto updateDto)
        {
            if (id != updateDto.Id)
            {
                return BadRequest("El ID de la ruta no coincide.");
            }

            // Incluimos las relaciones existentes para poder modificarlas
            var existingTimeInterval = await _context.AttTimeintervals
                .Include(t => t.AttTimeintervalBreakTimes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTimeInterval == null)
            {
                return NotFound();
            }

            // AutoMapper actualiza las propiedades principales (Alias, Duration, etc.)
            _mapper.Map(updateDto, existingTimeInterval);

            // --- LÓGICA PARA CALCULAR Y ASIGNAR MÁRGENES DE MARCACIÓN ---
            // (Esta es la misma lógica que en el método Create)
            if (!string.IsNullOrEmpty(updateDto.PunchInStartTime) && !string.IsNullOrEmpty(updateDto.PunchInEndTime))
            {
                var officialInTime = existingTimeInterval.InTime.TimeOfDay;
                var punchInStart = TimeSpan.Parse(updateDto.PunchInStartTime);
                var punchInEnd = TimeSpan.Parse(updateDto.PunchInEndTime);

                existingTimeInterval.InAheadMargin = (int)(officialInTime - punchInStart).TotalMinutes;
                existingTimeInterval.InAboveMargin = (int)(punchInEnd - officialInTime).TotalMinutes;
            }

            if (!string.IsNullOrEmpty(updateDto.PunchOutStartTime) && !string.IsNullOrEmpty(updateDto.PunchOutEndTime))
            {
                var scheduledOutTime = existingTimeInterval.InTime.AddMinutes(existingTimeInterval.Duration).TimeOfDay;
                var punchOutStart = TimeSpan.Parse(updateDto.PunchOutStartTime);
                var punchOutEnd = TimeSpan.Parse(updateDto.PunchOutEndTime);

                existingTimeInterval.OutAheadMargin = (int)(scheduledOutTime - punchOutStart).TotalMinutes;
                existingTimeInterval.OutAboveMargin = (int)(punchOutEnd - scheduledOutTime).TotalMinutes;
            }
            // --- FIN DE LA LÓGICA DE MÁRGENES ---

            // --- LÓGICA PARA ACTUALIZAR DESCANSOS (Eliminar y Re-crear) ---
            existingTimeInterval.AttTimeintervalBreakTimes.Clear();
            if (updateDto.BreakTimeIds != null && updateDto.BreakTimeIds.Any())
            {
                foreach (var breakId in updateDto.BreakTimeIds)
                {
                    existingTimeInterval.AttTimeintervalBreakTimes.Add(new AttTimeintervalBreakTime
                    {
                        BreaktimeId = breakId
                    });
                }
            }
            // --- FIN DE LA LÓGICA DE DESCANSOS ---

            existingTimeInterval.UpdatedAt = DateTime.Now;
            existingTimeInterval.UpdatedBy = User.Identity?.Name ?? "Sistema";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict($"Error al actualizar: {ex.InnerException?.Message}");
            }

            return NoContent();
        }


        /// <summary>
        /// Elimina un horario por su ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var timeInterval = await _context.AttTimeintervals.FindAsync(id);
            if (timeInterval == null)
            {
                return NotFound();
            }

            // Aquí necesitarás manejar la lógica de eliminación de relaciones si es necesario
            // por ejemplo, los descansos asociados, antes de eliminar el horario.

            _context.AttTimeintervals.Remove(timeInterval);
            await _context.SaveChangesAsync();

            return NoContent(); // Respuesta estándar 204 para una eliminación exitosa
        }
    }
}