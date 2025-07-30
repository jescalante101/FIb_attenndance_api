using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dtos.Manager;
using Entities.Manager;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Controllers.AppUserController
{
    /// <summary>
    /// Controlador para gestionar usuarios (AppUser).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AppUserController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del DbContext.
        /// </summary>
        public AppUserController(ApplicationDbcontext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los usuarios.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
        {
            var list = await _context.AppUsers.ToListAsync();
            if (list == null || !list.Any())
                return NotFound(new { message = "No se encontraron usuarios." });

            return Ok(list);
        }

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// </summary>
        [HttpGet("{userId:int}")]
        public async Task<ActionResult<AppUser>> GetById(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "El ID de usuario debe ser mayor que cero." });

            var entity = await _context.AppUsers.FindAsync(userId);

            if (entity == null)
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });

            return Ok(entity);
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(object))] // Ahora documenta el 201
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppUser>> Create([FromBody] AppUser appUser)
        {
            if (appUser == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (string.IsNullOrWhiteSpace(appUser.UserName))
                return BadRequest(new { message = "El nombre de usuario es obligatorio." });

            var exists = await _context.AppUsers.AnyAsync(u => u.UserName == appUser.UserName);
            if (exists)
                return Conflict(new { message = $"Ya existe un usuario con el nombre '{appUser.UserName}'." });

            _context.AppUsers.Add(appUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { userId = appUser.UserId }, new {message="Usuario creado con exito",data= appUser });
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        [HttpPut("{userId:int}")]
        public async Task<IActionResult> Update(int userId, [FromBody] AppUser updatedEntity)
        {
            if (updatedEntity == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (userId != updatedEntity.UserId)
                return BadRequest(new { message = "El ID de la ruta y el del cuerpo no coinciden." });

            var existing = await _context.AppUsers.FindAsync(userId);
            if (existing == null)
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });

            existing.UserName = updatedEntity.UserName;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el usuario.", details = ex.Message });
            }

            return Ok(existing);
        }

        /// <summary>
        /// Elimina un usuario por su ID.
        /// </summary>
        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> Delete(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "El ID de usuario debe ser mayor que cero." });

            var entity = await _context.AppUsers.FindAsync(userId);
            if (entity == null)
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });

            _context.AppUsers.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado correctamente." });
        }


        /// <summary>
        /// Obtiene las sedes y áreas asignadas a un usuario (consulta optimizada).
        /// </summary>
        [HttpGet("{userId}/sedes-areas")]
        public async Task<IActionResult> GetSedesYAreasDeUsuario(int userId)
        {
            // Validar existencia del usuario
            var usuario = await _context.AppUsers.FindAsync(userId);
            if (usuario == null)
                return NotFound(new { message = "No se encontró el usuario especificado." });

            // Un solo JOIN: AppUserSite + SiteArea (agrupando por sede)
            var query = from us in _context.AppUserSites
                        join sa in _context.SiteAreaCostCenters on us.SiteId equals sa.SiteId
                        where us.UserId == userId
                        select new
                        {
                            us.SiteId,
                            us.SiteName,
                            sa.AreaId,
                            sa.AreaName,
                            sa.CostCenterId,
                            sa.CostCenterName,
                        };

            var resultado = await query
                .GroupBy(x => new { x.SiteId, x.SiteName })
                .Select(g => new SedeConAreasDto
                {
                    SiteId = g.Key.SiteId,
                    SiteName = g.Key.SiteName,
                    Areas = g.Select(a => new AreaDto
                    {
                        AreaId = a.AreaId,
                        AreaName = a.AreaName
                    }).ToList(),
                    CostCenters = g.Select(a => new CostCenterDto
                    {
                        CostCenterId = a.CostCenterId,
                        CostCenterName = a.CostCenterName
                    }).Distinct().ToList()

                })
                .ToListAsync();

            if (!resultado.Any())
                return NotFound(new { message = "El usuario no tiene sedes ni áreas asignadas." });

            return Ok(resultado);
        }




    }
}
