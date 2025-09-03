
using AutoMapper;
using Dtos.ShiftDto;
using Entities.Manager;
using Entities.Shifts;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FibAttendanceApi.Controllers.ShiftController
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;
        public ShiftController(ApplicationDbcontext context,IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        [HttpGet("shifts")] // Ruta más estándar
        public async Task<ActionResult<IEnumerable<ShiftListDto>>> GetShifts(
         [FromQuery] int page = 1,
         [FromQuery] int pageSize = 10)
            {
                var totalRecords = await _context.AttAttshifts.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                var query = _context.AttAttshifts
                    .AsNoTracking() // Mejora el rendimiento para consultas de solo lectura
                    .Include(s => s.AttShiftdetails) // Carga eficiente de los detalles (Evita N+1)
                        .ThenInclude(sd => sd.TimeInterval); // Carga el TimeInterval anidado

                // Ordenamos y PAGINAMOS EN LA BASE DE DATOS
                var shiftsFromDb = await query
                    .OrderBy(s => s.Alias)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Mapeamos los resultados a nuestros DTOs limpios
                var shiftDtos = _mapper.Map<List<ShiftListDto>>(shiftsFromDb);

                return Ok(new
                {
                    data = shiftDtos,
                    totalRecords = totalRecords
                });
            }


        [HttpGet("shiftPorId/{id}")]
        public async Task<ActionResult> GetShiftById(int id)
        {
            var shift = await _context.AttAttshifts
                .Where(s => s.Id == id)
                 .Select(shift => new
                 {
                     shift.Id,
                     shift.Alias,
                     shift.ShiftCycle,
                     shift.CycleUnit,
                     shift.AutoShift,
                     shift.WorkDayOff,
                     shift.WeekendType,
                     horario = shift.AttShiftdetails
                         .Select(det => new
                         {
                             det.DayIndex,
                             det.TimeInterval.Alias,
                             det.TimeInterval.InTime,
                             det.TimeInterval.WorkTimeDuration
                         })
                         .Distinct()
                         .ToList()
                 })
                .FirstOrDefaultAsync();

            if (shift == null)
                return NotFound(new { mensaje = "Turno no encontrado." });

            return Ok(shift);
        }

        // DTOs específicos para evitar dynamic
        public class ShiftDetailDto
        {
            public int DayIndex { get; set; }
            public string Alias { get; set; }
            public DateTime InTime { get; set; }
            public int WorkTimeDuration { get; set; }
            public int Id { get; set; }
        }

        public class ShiftBaseDto
        {
            public int Id { get; set; }
            public string Alias { get; set; }
            public int ShiftCycle { get; set; }
            public int CycleUnit { get; set; }
            public bool AutoShift { get; set; }
            public bool WorkDayOff { get; set; }
            public int WeekendType { get; set; }
            public List<ShiftDetailDto> Horario { get; set; }
        }

        public class ExceptionDto
        {
            public int ExceptionId { get; set; }
            public DateTime StartDate { get; set; }
            public int TimeIntervalId { get; set; }
            public string TimeIntervalAlias { get; set; }
            public DateTime TimeIntervalInTime { get; set; }
            public int TimeIntervalWorkTimeDuration { get; set; }
        }

        // ENDPOINT 1: Horario para la semana actual
        [HttpGet("shiftWeekCurrent/{id}")]
        public async Task<ActionResult> GetShiftCurrentWeek(int id, [FromQuery] int? assignmentId = null)
        {
            var shift = await GetShiftBase(id);
            if (shift == null)
                return NotFound(new { mensaje = "Turno no encontrado." });

            if (!assignmentId.HasValue)
            {
                return Ok(new
                {
                    shift.Id,
                    shift.Alias,
                    shift.ShiftCycle,
                    shift.CycleUnit,
                    shift.AutoShift,
                    shift.WorkDayOff,
                    shift.WeekendType,
                    horario = shift.Horario.Select(h => new
                    {
                        h.DayIndex,
                        dayName = GetDayName(h.DayIndex % 7),
                        dayDate = (DateTime?)null,
                        h.Alias,
                        h.InTime,
                        h.WorkTimeDuration,
                        hasException = false,
                        exceptionId = (int?)null,
                        originalTimeIntervalId = h.Id
                    }).ToList()
                });
            }

            var assignment = await GetAssignment(assignmentId.Value);
            if (assignment == null)
                return NotFound(new { mensaje = $"No se encontró el assignment con ID {assignmentId.Value}" });

            if (assignment.ShiftId != id)
                return BadRequest(new { mensaje = $"El assignment {assignmentId.Value} no corresponde al turno {id}" });

            // Obtener la semana actual
            var today = DateTime.Now.Date;
            var currentWeekStart = GetStartOfWeek(today);
            var currentWeekEnd = currentWeekStart.AddDays(6);

            // Verificar que la semana actual esté dentro del rango del assignment
            if (currentWeekEnd < assignment.StartDate || currentWeekStart > assignment.EndDate)
            {
                return Ok(new
                {
                    message = "La semana actual está fuera del rango del assignment",
                    currentWeek = new { start = currentWeekStart, end = currentWeekEnd },
                    assignmentPeriod = new { start = assignment.StartDate, end = assignment.EndDate },
                    horario = new List<object>()
                });
            }

            // Ajustar la semana actual para que esté dentro del rango del assignment
            var effectiveWeekStart = currentWeekStart < assignment.StartDate ? assignment.StartDate : currentWeekStart;
            var effectiveWeekEnd = currentWeekEnd > assignment.EndDate ? assignment.EndDate : currentWeekEnd;

            var exceptions = await GetExceptions(assignmentId.Value, assignment.StartDate, assignment.EndDate);
            var horarioCurrentWeek = BuildWeekSchedule(shift.Horario, effectiveWeekStart, (DateTime)effectiveWeekEnd, exceptions);

            return Ok(new
            {
                shift.Id,
                shift.Alias,
                shift.ShiftCycle,
                shift.CycleUnit,
                shift.AutoShift,
                shift.WorkDayOff,
                shift.WeekendType,
                assignmentId = assignment.AssignmentId,
                employeeId = assignment.EmployeeId,
                assignmentPeriod = new { startDate = assignment.StartDate, endDate = assignment.EndDate },
                currentWeek = new { start = effectiveWeekStart, end = effectiveWeekEnd },
                horario = horarioCurrentWeek
            });
        }

        // ENDPOINT 2: Horario para todo el período del assignment
        [HttpGet("shiftPeriodComplete/{id}")]
        public async Task<ActionResult> GetShiftCompletePeriod(int id, [FromQuery] int? assignmentId = null)
        {
            var shift = await GetShiftBase(id);
            if (shift == null)
                return NotFound(new { mensaje = "Turno no encontrado." });

            if (!assignmentId.HasValue)
                return BadRequest(new { mensaje = "Se requiere assignmentId para obtener el período completo" });

            var assignment = await GetAssignment(assignmentId.Value);
            if (assignment == null)
                return NotFound(new { mensaje = $"No se encontró el assignment con ID {assignmentId.Value}" });

            if (assignment.ShiftId != id)
                return BadRequest(new { mensaje = $"El assignment {assignmentId.Value} no corresponde al turno {id}" });

            var exceptions = await GetExceptions(assignmentId.Value, assignment.StartDate, assignment.EndDate);
            var horarioCompleto = BuildCompletePeriodSchedule(shift.Horario, assignment.StartDate, (DateTime)assignment.EndDate, exceptions, shift.ShiftCycle);

            // Fix for CS1061: Ensure EndDate is not null before calculating Days  
            var totalDays = assignment.EndDate.HasValue
                ? (assignment.EndDate.Value - assignment.StartDate).Days + 1
                : 0;

            return Ok(new
            {
                shift.Id,
                shift.Alias,
                shift.ShiftCycle,
                shift.CycleUnit,
                shift.AutoShift,
                shift.WorkDayOff,
                shift.WeekendType,
                assignmentId = assignment.AssignmentId,
                employeeId = assignment.EmployeeId,
                assignmentPeriod = new { startDate = assignment.StartDate, endDate = assignment.EndDate },
                totalDays,
                horario = horarioCompleto
            });
        }

        private async Task<ShiftBaseDto> GetShiftBase(int id)
        {
            // PASO 1: Obtener la entidad principal (el turno) de forma simple.
            // Esto es una consulta básica que EF Core nunca falla en traducir.
            var shiftEntity = await _context.AttAttshifts
                .AsNoTracking() // Mejora el rendimiento ya que no necesitamos rastrear cambios.
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shiftEntity == null)
            {
                return null;
            }

            // PASO 2: Obtener los detalles relacionados en una consulta COMPLETAMENTE SEPARADA.
            // Incluimos TimeInterval para evitar más consultas a la BD después.
            var allShiftDetails = await _context.AttShiftdetails
                .AsNoTracking()
                .Include(det => det.TimeInterval) // ¡Importante! Carga los datos de TimeInterval.
                .Where(det => det.ShiftId == shiftEntity.Id) // Asumiendo que la FK se llama ShiftId.
                .ToListAsync();

            // PASO 3: Realizar la lógica de 'Distinct' (agrupación) EN MEMORIA.
            // Esto ya no se traduce a SQL, es C# puro y no puede fallar.
            var distinctDetails = allShiftDetails
                .GroupBy(det => det.TimeIntervalId)
                .Select(g => g.First())
                .ToList();

            // PASO 4: Construir manualmente el objeto DTO final con los datos que ya tenemos.
            // Esto tampoco involucra a la base de datos.
            var shiftDto = new ShiftBaseDto
            {
                Id = shiftEntity.Id,
                Alias = shiftEntity.Alias ?? "Sin nombre",
                ShiftCycle = shiftEntity.ShiftCycle,
                CycleUnit = shiftEntity.CycleUnit,
                AutoShift = shiftEntity.AutoShift,
                WorkDayOff = shiftEntity.WorkDayOff,
                WeekendType = shiftEntity.WeekendType,
                Horario = distinctDetails.Select(det => new ShiftDetailDto
                {
                    DayIndex = det.DayIndex,
                    Alias = det.TimeInterval.Alias ?? "Sin nombre",
                    InTime = det.TimeInterval.InTime,
                    WorkTimeDuration = det.TimeInterval.WorkTimeDuration,
                    Id = det.TimeInterval.Id
                }).ToList()
            };

            return shiftDto;
        }


        private async Task<EmployeeShiftAssignment> GetAssignment(int assignmentId)
        {
            return await _context.EmployeeScheduleAssignments
                .Where(a => a.AssignmentId == assignmentId)
                .FirstOrDefaultAsync();
        }

        private async Task<List<ExceptionDto>> GetExceptions(int assignmentId, DateTime startDate, DateTime? endDate)
        {
            var result = await (
                from ex in _context.EmployeeScheduleExceptions
                join ti in _context.AttTimeintervals on ex.TimeIntervalId equals ti.Id
                where ex.AssignmentId == assignmentId &&
                      ex.IsActive == 1 &&
                      ex.DayIndex.HasValue &&
                      ex.StartDate.HasValue &&
                      ex.StartDate.Value >= startDate &&
                      ex.StartDate.Value <= (endDate ?? DateTime.MaxValue) && // Handle nullable endDate
                      (ex.EndDate == null || ex.EndDate >= DateTime.Now.Date)
                select new ExceptionDto
                {
                    ExceptionId = ex.ExceptionId,
                    StartDate = ex.StartDate.Value,
                    TimeIntervalId = ex.TimeIntervalId,
                    TimeIntervalAlias = ti.Alias ?? "Sin nombre",
                    TimeIntervalInTime = ti.InTime,
                    TimeIntervalWorkTimeDuration = ti.WorkTimeDuration
                }
            ).ToListAsync();

            return result;
        }

        private List<object> BuildWeekSchedule(List<ShiftDetailDto> horarioBase, DateTime weekStart, DateTime weekEnd, List<ExceptionDto> exceptions)
        {
            var result = new List<object>();
            var exceptionsByDate = exceptions.ToDictionary(e => e.StartDate.Date, e => e);

            var current = weekStart;
            while (current <= weekEnd)
            {
                var dayOfWeek = (int)current.DayOfWeek;

                // Buscar el horario base que corresponde a este día
                var matchingSchedule = horarioBase.FirstOrDefault(h => h.DayIndex % 7 == dayOfWeek);

                if (matchingSchedule != null)
                {
                    var hasException = exceptionsByDate.ContainsKey(current);

                    if (hasException)
                    {
                        var exception = exceptionsByDate[current];
                        result.Add(new
                        {
                            dayIndex = matchingSchedule.DayIndex,
                            dayName = GetDayName(dayOfWeek),
                            dayDate = current,
                            alias = exception.TimeIntervalAlias,
                            inTime = exception.TimeIntervalInTime.ToString("HH:mm"),
                            workTimeDuration = exception.TimeIntervalWorkTimeDuration,
                            outTime=exception.TimeIntervalInTime.AddMinutes(exception.TimeIntervalWorkTimeDuration).ToString("HH:mm"),
                            hasException = true,
                            exceptionId = exception.ExceptionId,
                            exceptionDate = exception.StartDate,
                            originalTimeIntervalId = matchingSchedule.Id,
                            exceptionTimeIntervalId = exception.TimeIntervalId
                        });
                    }
                    else
                    {
                        result.Add(new
                        {
                            dayIndex = matchingSchedule.DayIndex,
                            dayName = GetDayName(dayOfWeek),
                            dayDate = current,
                            alias = matchingSchedule.Alias,
                            inTime = matchingSchedule.InTime.ToString("HH:mm"),
                            workTimeDuration = matchingSchedule.WorkTimeDuration,
                            outTime=matchingSchedule.InTime.AddMinutes(matchingSchedule.WorkTimeDuration).ToString("HH:mm"),
                            hasException = false,
                            exceptionId = (int?)null,
                            exceptionDate = (DateTime?)null,
                            originalTimeIntervalId = matchingSchedule.Id,
                            exceptionTimeIntervalId = (int?)null
                        });
                    }
                }

                current = current.AddDays(1);
            }

            return result;
        }

        private List<object> BuildCompletePeriodSchedule(List<ShiftDetailDto> horarioBase, DateTime startDate, DateTime endDate, List<ExceptionDto> exceptions, int shiftCycle)
        {
            var result = new List<object>();
            var exceptionsByDate = exceptions.ToDictionary(e => e.StartDate.Date, e => e);

            var current = startDate;
            while (current <= endDate)
            {
                var dayOfWeek = (int)current.DayOfWeek; // 0=Domingo, 1=Lunes, etc.

                // Buscar el horario base que corresponde a este día de la semana
                var matchingSchedule = horarioBase.FirstOrDefault(h => h.DayIndex == dayOfWeek);

                if (matchingSchedule != null)
                {
                    var hasException = exceptionsByDate.ContainsKey(current);

                    // Calcular el día del ciclo para información adicional
                    var daysSinceStart = (current - startDate).Days;
                    var cycleDay = daysSinceStart % shiftCycle + 1;

                    if (hasException)
                    {
                        var exception = exceptionsByDate[current];
                        result.Add(new
                        {
                            dayIndex = matchingSchedule.DayIndex,
                            cycleDay,
                            dayName = GetDayName(dayOfWeek),
                            dayDate = current,
                            alias = exception.TimeIntervalAlias,
                            inTime = exception.TimeIntervalInTime.ToString("HH:mm"),
                            workTimeDuration = exception.TimeIntervalWorkTimeDuration,
                            outTime=exception.TimeIntervalInTime.AddMinutes(exception.TimeIntervalWorkTimeDuration).ToString("HH:mm"),
                            hasException = true,
                            exceptionId = exception.ExceptionId,
                            exceptionDate = exception.StartDate,
                            originalTimeIntervalId = matchingSchedule.Id,
                            exceptionTimeIntervalId = exception.TimeIntervalId
                        });
                    }
                    else
                    {
                        result.Add(new
                        {
                            dayIndex = matchingSchedule.DayIndex,
                            cycleDay,
                            dayName = GetDayName(dayOfWeek),
                            dayDate = current,
                            alias = matchingSchedule.Alias,
                            inTime = matchingSchedule.InTime.ToString("HH:mm"),
                            workTimeDuration = matchingSchedule.WorkTimeDuration,
                            outTime=matchingSchedule.InTime.AddMinutes(matchingSchedule.WorkTimeDuration).ToString("HH:mm"),
                            hasException = false,
                            exceptionId = (int?)null,
                            exceptionDate = (DateTime?)null,
                            originalTimeIntervalId = matchingSchedule.Id,
                            exceptionTimeIntervalId = (int?)null
                        });
                    }
                }

                current = current.AddDays(1);
            }

            return result;
        }

        private string GetDayName(int dayIndex)
        {
            return dayIndex switch
            {
                0 => "Domingo",
                1 => "Lunes",
                2 => "Martes",
                3 => "Miércoles",
                4 => "Jueves",
                5 => "Viernes",
                6 => "Sábado",
                _ => "Desconocido"
            };
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }


        [HttpPost("nuevoShift")]
        public async Task<IActionResult> NuevoShift([FromBody] ShiftCreateDto dto)
        {
            var shift = new AttAttshift
            {
                Alias = dto.Alias,
                CycleUnit = dto.CycleUnit,
                ShiftCycle = dto.ShiftCycle,
                WorkWeekend = dto.WorkWeekend,
                WeekendType = dto.WeekendType,
                WorkDayOff = dto.WorkDayOff,
                DayOffType = dto.DayOffType,
                AutoShift = dto.AutoShift,
                CreatedAt = dto.CreatedAt ?? DateTime.Now,
                CreatedBy = dto.CreatedBy,
                AttShiftdetails = new List<AttShiftdetail>()
            };

            foreach (var det in dto.Detalles)
            {
                // Busca el intervalo para obtener el in_time y out_time
                var intervalo = await _context.AttTimeintervals.FindAsync(det.TimeIntervalId);
                if (intervalo == null)
                    return BadRequest($"No existe el intervalo con id {det.TimeIntervalId}");

                var detalle = new AttShiftdetail
                {
                    InTime = intervalo.InTime,
                    OutTime = intervalo.InTime.AddMinutes(intervalo.WorkTimeDuration),
                    DayIndex = det.DayIndex,
                    TimeIntervalId = det.TimeIntervalId
                    // ShiftId se asigna automáticamente al guardar el shift
                };
                shift.AttShiftdetails.Add(detalle);
            }

            _context.AttAttshifts.Add(shift);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Shift creado correctamente", id = shift.Id });
        }

        [HttpPut("actualizarShift/{id:int}")]
        public async Task<IActionResult> ActualizarShift(int id, [FromBody] ShiftUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var shift = await _context.AttAttshifts.FindAsync(id);
                if (shift == null)
                    return NotFound("Shift no encontrado");

                // 1. Actualizar propiedades del shift
                shift.Alias = dto.Alias;
                shift.CycleUnit = dto.CycleUnit;
                shift.ShiftCycle = dto.ShiftCycle;
                shift.WorkWeekend = dto.WorkWeekend;
                shift.WeekendType = dto.WeekendType;
                shift.WorkDayOff = dto.WorkDayOff;
                shift.DayOffType = dto.DayOffType;
                shift.AutoShift = dto.AutoShift;
                shift.UpdatedAt = dto.UpdatedAt ?? DateTime.Now;
                shift.UpdatedBy = dto.UpdatedBy;

                // 2. Eliminar detalles existentes con SQL crudo
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM att_shiftdetail WHERE shift_id = {0}", id);

                // 3. Guardar cambios del shift primero
                await _context.SaveChangesAsync();

                // 4. Insertar nuevos detalles UNO POR UNO (esto evita el MERGE)
                foreach (var det in dto.Detalles)
                {
                    var intervalo = await _context.AttTimeintervals.FindAsync(det.TimeIntervalId);
                    if (intervalo == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"No existe el intervalo con id {det.TimeIntervalId}");
                    }

                    var detalle = new AttShiftdetail
                    {
                        InTime = intervalo.InTime,
                        OutTime = intervalo.InTime.AddMinutes(intervalo.WorkTimeDuration),
                        DayIndex = det.DayIndex,
                        TimeIntervalId = det.TimeIntervalId,
                        ShiftId = id,
                        CreatedAt = DateTime.Now,
                        CreatedBy = dto.UpdatedBy ?? "system",
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = dto.UpdatedBy ?? "system"
                    };

                    // ← CLAVE: Add individual + SaveChanges individual
                    _context.AttShiftdetails.Add(detalle);
                    await _context.SaveChangesAsync(); // Guardar cada uno por separado
                }

                await transaction.CommitAsync();
                return Ok(new { mensaje = "Shift actualizado correctamente", id = shift.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { error = ex.Message, innerException = ex.InnerException?.Message });
            }
        }



        // ENDPOINT para eliminar un shift
        [HttpDelete("eliminarShift/{id:int}")]
        public async Task<IActionResult> EliminarShift(int id)
        {
            var shift = await _context.AttAttshifts
                .Include(s => s.AttShiftdetails) // Cargar los detalles relacionados
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shift == null)
                return NotFound("Shift no encontrado");

            // Eliminar los detalles asociados primero
            _context.AttShiftdetails.RemoveRange(shift.AttShiftdetails);

            // Eliminar el shift
            _context.AttAttshifts.Remove(shift);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Shift eliminado correctamente" });
        }

        // DTOs para debuggear y ver qué datos tenemos

    }
}
