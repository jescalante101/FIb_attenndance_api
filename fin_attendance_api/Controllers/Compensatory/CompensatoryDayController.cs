using AutoMapper;
using Dtos.Compensatory;
using Dtos.ResponseDto;
using Entities.Compensatory;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FibAttendanceApi.Controllers.Compensatory
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompensatoryDayController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public CompensatoryDayController(ApplicationDbcontext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedList<CompensatoryDayDto>>> GetCompensatoryDays([FromQuery] CompensatoryDayQueryParameters queryParameters)
        {
            var query = _context.CompensatoryDays.Include(cd => cd.EmployeeAssignment).AsQueryable();

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(cd =>
                    cd.EmployeeAssignment != null &&
                    (cd.EmployeeAssignment.FullNameEmployee.Contains(queryParameters.SearchTerm) ||
                     cd.EmployeeAssignment.NroDoc.Contains(queryParameters.SearchTerm)));
            }
            
            if (!string.IsNullOrEmpty(queryParameters.CompanyId))
            {
                query = query.Where(cd => cd.CompanyId == queryParameters.CompanyId);
            }

            if (queryParameters.StartDate.HasValue)
            {
                query = query.Where(cd => cd.HolidayWorkedDate >= queryParameters.StartDate.Value);
            }

            if (queryParameters.EndDate.HasValue)
            {
                query = query.Where(cd => cd.HolidayWorkedDate <= queryParameters.EndDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var items = await query.Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                                   .Take(queryParameters.PageSize)
                                   .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<CompensatoryDayDto>>(items);

            return new PaginatedList<CompensatoryDayDto>(dtos.ToList(), totalRecords, queryParameters.PageNumber, queryParameters.PageSize);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompensatoryDayDto>> GetCompensatoryDay(int id)
        {
            var compensatoryDay = await _context.CompensatoryDays.Include(cd => cd.EmployeeAssignment).FirstOrDefaultAsync(cd => cd.Id == id);

            if (compensatoryDay == null)
            {
                return NotFound();
            }

            return _mapper.Map<CompensatoryDayDto>(compensatoryDay);
        }

        [HttpPost]
        public async Task<ActionResult<CompensatoryDayDto>> PostCompensatoryDay(CreateCompensatoryDayDto createDto)
        {
            // --- INICIO DE LA VALIDACIÓN ---

            var alreadyExists = await _context.CompensatoryDays.AnyAsync(c =>
                      c.EmployeeId == createDto.EmployeeId &&
                      c.HolidayWorkedDate.Date == createDto.HolidayWorkedDate.Date &&
                      c.AssignmentId==createDto.AssignmentId
                      );

            if (alreadyExists)
            {
                // Si encontramos un duplicado, rechazamos toda la solicitud.
                return BadRequest($"El registro para el empleado '{createDto.EmployeeId}' en la fecha '{createDto.HolidayWorkedDate.ToShortDateString()}' ya existe.");
            }
            // --- FIN DE LA VALIDACIÓN ---


            // Si la validación pasa, el resto de tu código se ejecuta normalmente.
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "sistema";

            var compensatoryDay = _mapper.Map<CompensatoryDay>(createDto);
            compensatoryDay.CreatedBy = username;
            compensatoryDay.CreatedAt = DateTime.UtcNow;
            compensatoryDay.Status = "P"; // P for Pending

            _context.CompensatoryDays.Add(compensatoryDay);
            await _context.SaveChangesAsync();

            var dto = _mapper.Map<CompensatoryDayDto>(compensatoryDay);

            return CreatedAtAction(nameof(GetCompensatoryDay), new { id = dto.Id }, dto);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompensatoryDay(int id, UpdateCompensatoryDayDto updateDto)
        {
            var compensatoryDay = await _context.CompensatoryDays.FindAsync(id);

            if (compensatoryDay == null)
            {
                return NotFound();
            }
            
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "sistema";

            _mapper.Map(updateDto, compensatoryDay);
            compensatoryDay.UpdatedBy = username;
            compensatoryDay.UpdatedAt = DateTime.UtcNow;

            if(updateDto.Status == "A") // Approved
            {
                compensatoryDay.ApprovedBy = username;
                compensatoryDay.ApprovedAt = DateTime.UtcNow;
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CompensatoryDays.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompensatoryDay(int id)
        {
            var compensatoryDay = await _context.CompensatoryDays.FindAsync(id);
            if (compensatoryDay == null)
            {
                return NotFound();
            }

            _context.CompensatoryDays.Remove(compensatoryDay);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpPost("bulk")]
        public async Task<ActionResult<List<CompensatoryDayDto>>> PostBulkCompensatoryDays(
    [FromBody] List<CreateCompensatoryDayDto> createDtos)
        {
            if (createDtos == null || !createDtos.Any())
            {
                return BadRequest("La lista de días compensatorios a crear no puede estar vacía.");
            }

            try
            {
                var compensatoryDaysToAdd = new List<CompensatoryDay>();
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "sistema";
                var creationTime = DateTime.UtcNow;

                // Bucle para validar y preparar las entidades
                foreach (var dto in createDtos)
                {
                    // ===== VALIDACIÓN DE DUPLICADOS =====
                    // Comprueba si ya existe un registro para el mismo empleado y el mismo día feriado trabajado.
                    // Usamos .Date para ignorar la parte de la hora en la comparación.
                    var alreadyExists = await _context.CompensatoryDays.AnyAsync(c =>
                        c.EmployeeId == dto.EmployeeId &&
                        c.HolidayWorkedDate.Date == dto.HolidayWorkedDate.Date);

                    if (alreadyExists)
                    {
                        // Si encontramos un duplicado, rechazamos toda la solicitud.
                        return BadRequest($"El registro para el empleado '{dto.EmployeeId}' en la fecha '{dto.HolidayWorkedDate.ToShortDateString()}' ya existe.");
                    }
                    // =====================================

                    // Si pasa la validación, preparamos la entidad para agregarla
                    var newDay = _mapper.Map<CompensatoryDay>(dto);
                    newDay.CreatedBy = username;
                    newDay.CreatedAt = creationTime;
                    newDay.Status = "P";
                    compensatoryDaysToAdd.Add(newDay);
                }

                // Si todos los registros son válidos, los agregamos y guardamos
                _context.CompensatoryDays.AddRange(compensatoryDaysToAdd);
                await _context.SaveChangesAsync();

                var resultDtos = _mapper.Map<List<CompensatoryDayDto>>(compensatoryDaysToAdd);
                return Ok(resultDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al procesar el registro masivo: {ex.Message}");
            }
        }

    }
}