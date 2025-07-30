using AutoMapper;
using Dtos.TimeIntervalDto;
using Entities.Shifts;
using FibAttendanceApi.Data;
using FibAttendanceApi.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace FibAttendanceApi.Controllers.ShiftController
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttTimeIntervalController
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public AttTimeIntervalController(ApplicationDbcontext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        [HttpGet("lstHorarios")]
        public async Task<ActionResult<IEnumerable<AttTimeinterval>>> getListShiftDetails([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
        {
            var totalTransactions = await _context.AttTimeintervals.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);

            if (page < 1 || page > totalPages)
            {
                return new BadRequestObjectResult("Número de página inválido.");
            }

            var lstShiftDetails = await _context.AttTimeintervals
                .Skip((page - 1) * pageSize)
                 .OrderByDescending(h => h.Id)
            .Take(pageSize)
                .ToListAsync();

            return new OkObjectResult(new
            {
                data = lstShiftDetails,
                totalRecords = totalTransactions
            });
        }


        [HttpGet("lstHoraiosMap")]
        public async Task<ActionResult<IEnumerable<HorarioInfoDto>>> getHorarioslst([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
        {
            var totalTransactions = await _context.AttTimeintervals.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);

            if (page < 1 || page > totalPages)
            {
                return new BadRequestObjectResult("Número de página inválido.");
            }

            var lstShiftDetails = await _context.AttTimeintervals
                .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(h => h.AttTimeintervalBreakTimes)
            .ThenInclude(br => br.Breaktime)
                .ToListAsync();
            var newDate = _mapper.Map<List<HorarioInfoDto>>(lstShiftDetails);

            return new OkObjectResult(new
            {
                data = newDate,
                totalRecords = totalTransactions
            });
        }


        [HttpGet("horarioPorId/{id}")]
        public async Task<ActionResult<HorarioInfoDto>> findById(int id)
        {

            var horario = await _context.AttTimeintervals
                             .Include(h => h.AttTimeintervalBreakTimes) // Cargar la colección
                                 .ThenInclude(bt => bt.Breaktime)     // Y luego la relación dentro de la colección
                             .Where(t => t.Id == id)
                             .FirstOrDefaultAsync();

            if (horario == null)
            {
                return new NotFoundResult();
            }

            var newData = _mapper.Map<HorarioInfoDto>(horario);

            var breaktime = await _context.AttTimeintervalBreakTimes.Where(t => t.TimeintervalId == id).FirstOrDefaultAsync();


            return new OkObjectResult(new
            {
                horario = newData,
                BreaktimeId = breaktime?.BreaktimeId ?? 0

            });
        }



        [HttpPost("nuevoHorario")]
        public async Task<ActionResult<AttTimeinterval>> GuardarHorario([FromBody] HorarioInfoDto attTimeinterval)
        {
            AttTimeinterval interval = new AttTimeinterval();

            if (attTimeinterval.Tipo == 0)
            {



                interval.Alias = attTimeinterval.Nombre;
                interval.UseMode = short.Parse(attTimeinterval.Tipo.ToString());
                interval.InTime = Utils.ParsearFechaHora(attTimeinterval.HoraEntrada);

                var tolerancias = Utils.ObtenerTolerancias(
                    attTimeinterval.HoraEntrada,
                    attTimeinterval.HoraSalida,
                    attTimeinterval.HoraEntradaDesde,
                    attTimeinterval.HoraEntradaHasta,
                    attTimeinterval.HoraSalidaDesde,
                    attTimeinterval.HoraSalidaHasta
                );

                interval.InAheadMargin = Convert.ToInt32(tolerancias.ToleranciaEntradaDesdeMinutos);
                interval.InAboveMargin = Convert.ToInt32(tolerancias.ToleranciaEntradaHastaMinutos);
                interval.OutAheadMargin = Convert.ToInt32(tolerancias.ToleranciaSalidaDesdeMinutos);
                interval.OutAboveMargin = Convert.ToInt32(tolerancias.ToleranciaSalidaHastaMinutos);
                interval.Duration = Convert.ToInt32(tolerancias.TiempoTrabajoMinutos);
                interval.InRequired = Convert.ToInt32(attTimeinterval.MarcarEntrada) == 0 ? (short)0 : (short)1;
                interval.OutRequired = Convert.ToInt32(attTimeinterval.MarcarSalida) == 0 ? (short)0 : (short)1;
                interval.AllowLate = Convert.ToInt32(attTimeinterval.PSalidaT);
                interval.AllowLeaveEarly = Convert.ToInt32(attTimeinterval.PLlegadaT);
                interval.AvailableIntervalType = (short)attTimeinterval.TipoIntervalo;
                interval.WorkDay = Convert.ToDouble(attTimeinterval.DiasLaboral);
                interval.MultiplePunch = (short)attTimeinterval.BasadoM;
                interval.AvailableInterval = attTimeinterval.PeriodoMarcacion;
                interval.WorkType = 0;

                interval.DayChange = DateTime.Today;


                interval.OvertimeLv = (short)attTimeinterval.Hnivel;
                interval.OvertimeLv1 = (short)attTimeinterval.HNivel1;
                interval.OvertimeLv2 = (short)attTimeinterval.HNivel2;
                interval.OvertimeLv3 = (short)attTimeinterval.HNivel3;
                interval.WorkTimeDuration = Convert.ToInt32(tolerancias.TiempoTrabajoMinutos);
                interval.MinEarlyIn = Convert.ToInt32(attTimeinterval.MinEntradaTemprana);
                interval.MinLateOut = Convert.ToInt32(attTimeinterval.MinSalidaTarde);
                interval.LateOut = (short)Convert.ToInt32(attTimeinterval.EntradaTarde);
                interval.EarlyIn = (short)Convert.ToInt32(attTimeinterval.EntradaTemprana);
                interval.TotalMarkings = attTimeinterval.TotalMarcaciones;
                interval.AttTimeintervalBreakTimes = new List<AttTimeintervalBreakTime>();

                // Fix: Ensure Descanso is treated as a collection of integers
                var breakTimeIds = new List<int> { attTimeinterval.Descanso }; // Assuming Descanso is a single int, wrap it in a list
                foreach (var breakTimeId in breakTimeIds)
                {
                    var breakTimeEntity = await _context.AttBreaktimes.FindAsync(breakTimeId);
                    if (breakTimeEntity != null)
                    {
                        var attTimeintervalBreakTime = new AttTimeintervalBreakTime
                        {
                            BreaktimeId = breakTimeId,
                            TimeintervalId = interval.Id
                        };
                        interval.AttTimeintervalBreakTimes.Add(attTimeintervalBreakTime);
                    }
                }
                _context.AttTimeintervals.Add(interval);
                await _context.SaveChangesAsync();

                // Reemplaza la línea problemática en el método GuardarHorario:
                // return new  CreatedAtActionResult(nameof(getHorarioslst), new { id = interval.Id });

                // Por la siguiente línea, que incluye todos los argumentos requeridos:
                return new CreatedAtActionResult(
                    actionName: nameof(getHorarioslst),
                    controllerName: null,
                    routeValues: new { id = interval.Id },
                    value: null
                );
                
            }
            else
            {
                interval.Alias = attTimeinterval.Nombre;
                interval.UseMode = short.Parse(attTimeinterval.Tipo.ToString());
                interval.InTime = Utils.ParsearFechaHora(attTimeinterval.HoraEntrada);
                interval.WorkTimeDuration = Utils.ObtenerDiferenciaEnMinutos(attTimeinterval.HoraEntrada, attTimeinterval.HoraSalida);
                interval.EarlyIn = (short)Convert.ToInt32(attTimeinterval.EntradaTemprana);
                interval.MinEarlyIn = (short)Convert.ToInt32(attTimeinterval.MinEntradaTemprana);
                interval.OvertimeLv = (short)attTimeinterval.Hnivel;
                interval.OvertimeLv1 = (short)attTimeinterval.HNivel1;
                interval.OvertimeLv2 = (short)attTimeinterval.HNivel2;
                interval.OvertimeLv3 = (short)attTimeinterval.HNivel3;

                interval.InRequired = Convert.ToInt32(attTimeinterval.MarcarEntrada) == 0 ? (short)0 : (short)1;
                interval.OutRequired = Convert.ToInt32(attTimeinterval.MarcarSalida) == 0 ? (short)0 : (short)1;
                interval.AvailableIntervalType = (short)attTimeinterval.TipoIntervalo;
                interval.AvailableInterval = attTimeinterval.PeriodoMarcacion;
                interval.MultiplePunch = (short)attTimeinterval.BasadoM;
                interval.TotalMarkings = attTimeinterval.TotalMarcaciones;

                interval.DayChange = DateTime.Today;

                _context.AttTimeintervals.Add(interval);
                await _context.SaveChangesAsync();

                // Reemplaza la línea:
                // return CreatedAtAction(nameof(getHorarioslst), new { id = interval.Id });

                // Por la siguiente línea, que utiliza el constructor de CreatedAtActionResult directamente:
                return new CreatedAtActionResult(
                    actionName: nameof(getHorarioslst),
                    controllerName: null,
                    routeValues: new { id = interval.Id },
                    value: null
                );
              

            }
        }

        [HttpPut("actualizarHorario/")]
        public async Task<ActionResult<AttTimeinterval>> ActualizarHorario([FromBody] HorarioInfoDto attTimeinterval)
        {
            var interval = await _context.AttTimeintervals
                .Include(i => i.AttTimeintervalBreakTimes)
                .FirstOrDefaultAsync(i => i.Id == attTimeinterval.IdHorio);

            if (interval == null)
                return new NotFoundObjectResult(new { mensaje = "Horario no encontrado." });

            interval.Alias = attTimeinterval.Nombre;
            interval.UseMode = short.Parse(attTimeinterval.Tipo.ToString());
            interval.InTime = Utils.ParsearFechaHora(attTimeinterval.HoraEntrada);

            var tolerancias = Utils.ObtenerTolerancias(
                attTimeinterval.HoraEntrada,
                attTimeinterval.HoraSalida,
                attTimeinterval.HoraEntradaDesde,
                attTimeinterval.HoraEntradaHasta,
                attTimeinterval.HoraSalidaDesde,
                attTimeinterval.HoraSalidaHasta
            );

            interval.InAheadMargin = Convert.ToInt32(tolerancias.ToleranciaEntradaDesdeMinutos);
            interval.InAboveMargin = Convert.ToInt32(tolerancias.ToleranciaEntradaHastaMinutos);
            interval.OutAheadMargin = Convert.ToInt32(tolerancias.ToleranciaSalidaDesdeMinutos);
            interval.OutAboveMargin = Convert.ToInt32(tolerancias.ToleranciaSalidaHastaMinutos);
            interval.Duration = Convert.ToInt32(tolerancias.TiempoTrabajoMinutos);
            interval.InRequired = Convert.ToInt32(attTimeinterval.MarcarEntrada) == 0 ? (short)0 : (short)1;
            interval.OutRequired = Convert.ToInt32(attTimeinterval.MarcarSalida) == 0 ? (short)0 : (short)1;
            interval.AllowLate = Convert.ToInt32(attTimeinterval.PSalidaT);
            interval.AllowLeaveEarly = Convert.ToInt32(attTimeinterval.PLlegadaT);
            interval.AvailableIntervalType = (short)attTimeinterval.TipoIntervalo;
            interval.WorkDay = Convert.ToDouble(attTimeinterval.DiasLaboral);
            interval.MultiplePunch = (short)attTimeinterval.BasadoM;
            interval.AvailableInterval = attTimeinterval.PeriodoMarcacion;
            interval.WorkType = 0;
            interval.DayChange = DateTime.Today;
            interval.OvertimeLv = (short)attTimeinterval.Hnivel;
            interval.OvertimeLv1 = (short)attTimeinterval.HNivel1;
            interval.OvertimeLv2 = (short)attTimeinterval.HNivel2;
            interval.OvertimeLv3 = (short)attTimeinterval.HNivel3;
            interval.WorkTimeDuration = Convert.ToInt32(tolerancias.TiempoTrabajoMinutos);
            interval.MinEarlyIn = Convert.ToInt32(attTimeinterval.MinEntradaTemprana);
            interval.MinLateOut = Convert.ToInt32(attTimeinterval.MinSalidaTarde);
            interval.LateOut = (short)Convert.ToInt32(attTimeinterval.EntradaTarde);
            interval.EarlyIn = (short)Convert.ToInt32(attTimeinterval.EntradaTemprana);
            interval.TotalMarkings = (short)Convert.ToInt32(attTimeinterval.TotalMarcaciones);

            // Actualizar descansos
            if (interval.AttTimeintervalBreakTimes != null)
                _context.AttTimeintervalBreakTimes.RemoveRange(interval.AttTimeintervalBreakTimes);

            interval.AttTimeintervalBreakTimes = new List<AttTimeintervalBreakTime>();
            var breakTimeIds = new List<int> { attTimeinterval.Descanso };
            foreach (var breakTimeId in breakTimeIds)
            {
                var breakTimeEntity = await _context.AttBreaktimes.FindAsync(breakTimeId);
                if (breakTimeEntity != null)
                {
                    var attTimeintervalBreakTime = new AttTimeintervalBreakTime
                    {
                        BreaktimeId = breakTimeId,
                        TimeintervalId = interval.Id
                    };
                    interval.AttTimeintervalBreakTimes.Add(attTimeintervalBreakTime);
                }
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult(new
            {
                message = "Actualizado co Exito",
                id = attTimeinterval.IdHorio
            });
        }


        [HttpDelete("eliminarHorario/{id}")]
        public async Task<IActionResult> EliminarHorario(int id)
        {
            var horario = await _context.AttTimeintervals
                .Include(h => h.AttTimeintervalBreakTimes)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null)
            {
                return new NotFoundObjectResult(new { mensaje = "Horario no encontrado." });
            }

            // Eliminar relaciones en AttTimeintervalBreakTime
            if (horario.AttTimeintervalBreakTimes != null && horario.AttTimeintervalBreakTimes.Any())
            {
                _context.AttTimeintervalBreakTimes.RemoveRange(horario.AttTimeintervalBreakTimes);
            }

            _context.AttTimeintervals.Remove(horario);
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { mensaje = "Horario eliminado correctamente." });
        }

    }
}
