using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Dtos.Personal;
using FibAttendanceApi.Data;
using Entities.Personal; // Asegúrate de que este 'using' apunte a tu DTO

namespace FibAttendanceApi.Controllers.Personal
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonnelWhitelistController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public PersonnelWhitelistController(ApplicationDbcontext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/PersonnelWhitelist
        /// <summary>
        /// Obtiene al personal de la whitelist con paginación, filtros y ordenamiento.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PersonnelWhitelistDto>>> GetPersonnelWhitelists([FromQuery] PaginationFilterPerosalWLDto filter)
        {
            var queryable = _context.PersonnelWhitelists.AsQueryable();

            // Aplicar filtro de texto
            if (!string.IsNullOrWhiteSpace(filter.FilterText))
            {
                var searchText = filter.FilterText.ToLower().Trim();
                queryable = queryable.Where(p =>
                    p.EmployeeId.ToLower().Contains(searchText) ||
                    p.EmployeeName.ToLower().Contains(searchText) ||
                    (p.Remarks != null && p.Remarks.ToLower().Contains(searchText))
                );
            }

            // Aplicar ordenamiento
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                switch (filter.SortBy.ToLowerInvariant())
                {
                    case "employeename":
                        queryable = filter.IsAscending ? queryable.OrderBy(p => p.EmployeeName) : queryable.OrderByDescending(p => p.EmployeeName);
                        break;
                    case "employeeid":
                        queryable = filter.IsAscending ? queryable.OrderBy(p => p.EmployeeId) : queryable.OrderByDescending(p => p.EmployeeId);
                        break;
                    case "createdat":
                        queryable = filter.IsAscending ? queryable.OrderBy(p => p.CreatedAt) : queryable.OrderByDescending(p => p.CreatedAt);
                        break;
                    default:
                        queryable = queryable.OrderByDescending(p => p.CreatedAt);
                        break;
                }
            }
            else
            {
                // Orden por defecto
                queryable = queryable.OrderByDescending(p => p.CreatedAt);
            }

            var totalCount = await queryable.CountAsync();

            var items = await queryable
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectTo<PersonnelWhitelistDto>(_mapper.ConfigurationProvider) // Mapeo eficiente
                .ToListAsync();

            var pagedResult = new PagedResultDto<PersonnelWhitelistDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return Ok(pagedResult);
        }

        // GET: api/PersonnelWhitelist/5
        /// <summary>
        /// Obtiene a una persona específica de la whitelist por su ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonnelWhitelistDto>> GetPersonnelWhitelist(int id)
        {
            var personnelWhitelist = await _context.PersonnelWhitelists.FindAsync(id);

            if (personnelWhitelist == null)
            {
                return NotFound();
            }

            return _mapper.Map<PersonnelWhitelistDto>(personnelWhitelist);
        }

        // PUT: api/PersonnelWhitelist/5
        /// <summary>
        /// Actualiza a una persona específica en la whitelist.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPersonnelWhitelist(int id, PersonnelWhitelistCreateEditDto dto)
        {
            var personnelToUpdate = await _context.PersonnelWhitelists.FindAsync(id);

            if (personnelToUpdate == null)
            {
                return NotFound($"No personnel found with ID {id}.");
            }

            var currentUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            _mapper.Map(dto, personnelToUpdate);
            personnelToUpdate.UpdatedBy = currentUser;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonnelWhitelistExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // POST: api/PersonnelWhitelist
        /// <summary>
        /// Crea una nueva persona en la whitelist.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PersonnelWhitelistDto>> PostPersonnelWhitelist(PersonnelWhitelistCreateEditDto dto)
        {
            var newPersonnel = _mapper.Map<PersonnelWhitelist>(dto);
            var currentUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            newPersonnel.CreatedBy = currentUser;
           // newPersonnel.UpdatedBy = currentUser;

            _context.PersonnelWhitelists.Add(newPersonnel);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<PersonnelWhitelistDto>(newPersonnel);

            return CreatedAtAction(nameof(GetPersonnelWhitelist), new { id = newPersonnel.Id }, resultDto);
        }

        // POST: api/PersonnelWhitelist/bulk
        /// <summary>
        /// Crea múltiples registros de personal en la whitelist de forma masiva.
        /// </summary>
        [HttpPost("bulk")]
        public async Task<IActionResult> PostBulkPersonnelWhitelist([FromBody] IEnumerable<PersonnelWhitelistCreateEditDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest("La lista de personal no puede ser nula o vacía.");
            }

            var currentUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            var personnelList = _mapper.Map<IEnumerable<PersonnelWhitelist>>(dtos);

            foreach (var person in personnelList)
            {
                person.CreatedBy = currentUser;
               // person.UpdatedBy = currentUser;
            }

            await _context.PersonnelWhitelists.AddRangeAsync(personnelList);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{personnelList.Count()} registros creados exitosamente." });
        }

        

        // DELETE: api/PersonnelWhitelist/5
        /// <summary>
        /// Elimina a una persona de la whitelist.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonnelWhitelist(int id)
        {
            var personnelWhitelist = await _context.PersonnelWhitelists.FindAsync(id);
            if (personnelWhitelist == null)
            {
                return NotFound();
            }

            _context.PersonnelWhitelists.Remove(personnelWhitelist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PersonnelWhitelistExists(int id)
        {
            return _context.PersonnelWhitelists.Any(e => e.Id == id);
        }
    }
}

