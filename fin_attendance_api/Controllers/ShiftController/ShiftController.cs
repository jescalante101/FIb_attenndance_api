
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
        public ShiftController(ApplicationDbcontext context)
        {
            _context = context;
        }


        [HttpGet("lstShifts")]
        public async Task<ActionResult<IEnumerable<AttAttshift>>> getAttShift([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var totalTransactions = await _context.AttAttshifts.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);

            if (page < 1 || page > totalPages)
            {
                return BadRequest("Número de páginas inválido");
            }

            var lstAttShift = await _context.AttAttshifts
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
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();


            return Ok(
                new
                {
                    data = lstAttShift,
                    totalRecords = totalTransactions
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
            var shift = await _context.AttAttshifts
                .Where(s => s.Id == id)
                .Select(shift => new ShiftBaseDto
                {
                    Id = shift.Id,
                    Alias = shift.Alias ?? "Sin nombre",
                    ShiftCycle = shift.ShiftCycle,
                    CycleUnit = shift.CycleUnit,
                    AutoShift = shift.AutoShift,
                    WorkDayOff = shift.WorkDayOff,
                    WeekendType = shift.WeekendType,
                    Horario = shift.AttShiftdetails
                        .Select(det => new ShiftDetailDto
                        {
                            DayIndex = det.DayIndex,
                            Alias = det.TimeInterval.Alias ?? "Sin nombre",
                            InTime = det.TimeInterval.InTime,
                            WorkTimeDuration = det.TimeInterval.WorkTimeDuration,
                            Id = det.TimeInterval.Id
                        })
                        .Distinct()
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return shift;
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
            var shift = await _context.AttAttshifts
                                      .Include(s => s.AttShiftdetails)
                                      .FirstOrDefaultAsync(s => s.Id == id);

            if (shift == null)
                return NotFound("Shift no encontrado");

            // 1. Actualizo propiedades simples
            shift.Alias = dto.Alias;
            shift.CycleUnit = dto.CycleUnit;
            shift.ShiftCycle = dto.ShiftCycle;
            shift.WorkWeekend = dto.WorkWeekend;
            shift.WeekendType = dto.WeekendType;
            shift.WorkDayOff = dto.WorkDayOff;
            shift.DayOffType = dto.DayOffType;
            shift.AutoShift = dto.AutoShift;

            // 2. Elimino los detalles actuales
            _context.AttShiftdetails.RemoveRange(shift.AttShiftdetails);

            // 3. Agrego los nuevos
            foreach (var det in dto.Detalles)
            {
                var intervalo = await _context.AttTimeintervals.FindAsync(det.TimeIntervalId);
                if (intervalo == null)
                    return BadRequest($"No existe el intervalo con id {det.TimeIntervalId}");

                var detalle = new AttShiftdetail
                {
                    InTime = intervalo.InTime,
                    OutTime = intervalo.InTime.AddMinutes(intervalo.WorkTimeDuration),
                    DayIndex = det.DayIndex,
                    TimeIntervalId = det.TimeIntervalId,
                    ShiftId = shift.Id     // explícito
                };
                shift.AttShiftdetails.Add(detalle);
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Shift actualizado correctamente", id = shift.Id });
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
