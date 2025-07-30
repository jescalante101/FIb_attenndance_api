using AutoMapper;
using Dtos.ShiftDto;
using Entities.Shifts;
using FibAttendanceApi.Data;
using FibAttendanceApi.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Controllers.ShiftController
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttBreaktimeController: ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public AttBreaktimeController(ApplicationDbcontext context,IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        [HttpGet("lstDescansos")]
        public async Task<ActionResult<IEnumerable<AttBreaktime>>> getAttBreakTime([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var totalTransactions = await _context.AttBreaktimes.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);

            if (page < 1 || page > totalPages)
            {
                return BadRequest("Número de páginas inválido");
            }

            var lstAttBreaksTimes = await _context.AttBreaktimes.
                Skip((page - 1) / pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(
                   new
                   {
                       data = lstAttBreaksTimes,
                       totalRecords = totalTransactions
                   }
                );


        }

        [HttpGet("descansoPorId/{id}")]
        public async Task<ActionResult<BreakTimeInfoDto>> getAttBreakTimeById(int id)
        {
            var descanso = await _context.AttBreaktimes
                .Where(d => d.Id == id)
                .FirstOrDefaultAsync();
            if (descanso == null)
            {
                return NotFound(new { mensaje = "Descanso no encontrado." });
            }
            var descansoDto = _mapper.Map<BreakTimeInfoDto>(descanso);
            return Ok(descansoDto);
        }

        [HttpPost("nuevoDescanso")]
        public async Task<ActionResult<BreakTimeInfoDto>> nuevoDescanso([FromBody] BreakTimeInfoDto descansoInfo)
        {
            AttBreaktime breaktime = new AttBreaktime();
            breaktime.Alias = descansoInfo.Alias;
            breaktime.PeriodStart = Utils.ParsearFechaHora(descansoInfo.PeriodStart);
            breaktime.Duration = descansoInfo.Duration;
            breaktime.AvailableIntervalType = (short)descansoInfo.AvailableIntervalType;
            breaktime.AvailableInterval = (short)descansoInfo.AvailableInterval;
            breaktime.EndMargin = Utils.ObtenerDiferenciaEnMinutos(descansoInfo.PeriodStart, descansoInfo.EndMargin);
            breaktime.LateIn = (short)descansoInfo.LateIn;
            breaktime.MinLateIn = (short)descansoInfo.MinLateIn;
            breaktime.MinEarlyIn = (short)descansoInfo.MinEarlyIn;
            breaktime.MultiplePunch = (short)descansoInfo.MultiplePunch;
            _context.AttBreaktimes.Add(breaktime);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Horario eliminado correctamente." });
        }

        [HttpPut("actualizarDescanso/")]
        public async Task<ActionResult<BreakTimeInfoDto>> ActualizarDescanso([FromBody] BreakTimeInfoDto descansoInfo)
        {
            var descanso = await _context.AttBreaktimes
                .FirstOrDefaultAsync(d => d.Id == descansoInfo.Id);
            if (descanso == null)
                return NotFound(new { mensaje = "Descanso no encontrado." });
            descanso.Alias = descansoInfo.Alias;
            descanso.PeriodStart = Utils.ParsearFechaHora(descansoInfo.PeriodStart);
            descanso.Duration = descansoInfo.Duration;
            descanso.AvailableIntervalType = (short)descansoInfo.AvailableIntervalType;
            descanso.AvailableInterval = (short)descansoInfo.AvailableInterval;
            descanso.EndMargin = Utils.ObtenerDiferenciaEnMinutos(descansoInfo.PeriodStart, descansoInfo.EndMargin);
            descanso.LateIn = (short)descansoInfo.LateIn;
            descanso.MinLateIn = (short)descansoInfo.MinLateIn;
            descanso.MinEarlyIn = (short)descansoInfo.MinEarlyIn;
            descanso.MultiplePunch = (short)descansoInfo.MultiplePunch;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Actualizado con éxito." });
        }

        [HttpDelete("eliminarDescanso/{id}")]
        public async Task<IActionResult> EliminarDescanso(int id)
        {
            var descanso = await _context.AttBreaktimes
                .FirstOrDefaultAsync(d => d.Id == id);
            if (descanso == null)
            {
                return NotFound(new { mensaje = "Descanso no encontrado." });
            }
            _context.AttBreaktimes.Remove(descanso);
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Descanso eliminado correctamente." });
        }
    }
}
