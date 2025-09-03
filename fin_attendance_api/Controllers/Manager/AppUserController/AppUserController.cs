using AutoMapper;
using Dtos.Manager;
using Entities.Manager;
using FibAttendanceApi.Data;
using FibAttendanceApi.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FibAttendanceApi.Controllers.Manager
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Added Authorize at class level
    public class AppUserController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AppUserController(ApplicationDbcontext context, IPasswordService passwordService, ITokenService tokenService, IMapper mapper)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpGet]
        // [Authorize] // Removed as it's now at class level
        public async Task<ActionResult<IEnumerable<AppUserDto>>> GetAll()
        {
            var list = await _context.AppUsers.ToListAsync();
            if (list == null || !list.Any())
                return NotFound(new { message = "No se encontraron usuarios." });

            var listDto = _mapper.Map<IEnumerable<AppUserDto>>(list);
            return Ok(listDto);
        }

        [HttpGet("{userId:int}")]
        // [Authorize] // Removed as it's now at class level
        public async Task<ActionResult<AppUserDto>> GetById(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "El ID de usuario debe ser mayor que cero." });

            var entity = await _context.AppUsers.FindAsync(userId);

            if (entity == null)
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });

            var entityDto = _mapper.Map<AppUserDto>(entity);
            return Ok(entityDto);
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppUserDto>> Create([FromBody] AppUserCreateDto appUserDto)
        {
            if (appUserDto == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            var exists = await _context.AppUsers.AnyAsync(u => u.UserName == appUserDto.UserName || u.Email == appUserDto.Email);
            if (exists)
                return Conflict(new { message = $"Ya existe un usuario con el nombre '{appUserDto.UserName}' o email '{appUserDto.Email}'." });

            var appUser = new AppUser
            {
                UserName = appUserDto.UserName,
                Email = appUserDto.Email,
                PasswordHash = _passwordService.HashPassword(appUserDto.Password),
                FirstName = appUserDto.FirstName,
                LastName = appUserDto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = appUserDto.CreatedBy
            };

            _context.AppUsers.Add(appUser);
            await _context.SaveChangesAsync();

            var appUserDtoResult = _mapper.Map<AppUserDto>(appUser);

            return CreatedAtAction(nameof(GetById), new { userId = appUser.UserId }, new { message = "Usuario creado con exito", data = appUserDtoResult });
        }

        [HttpPut("{userId:int}")]
        // No need for [Authorize] here, as it's covered by class-level Authorize
        public async Task<IActionResult> Update(int userId, [FromBody] AppUserUpdateDto updatedEntity)
        {
            if (updatedEntity == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            var existing = await _context.AppUsers.FindAsync(userId);
            if (existing == null)
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });

            if (!string.IsNullOrWhiteSpace(updatedEntity.UserName))
                existing.UserName = updatedEntity.UserName;

            if (!string.IsNullOrWhiteSpace(updatedEntity.Email))
                existing.Email = updatedEntity.Email;

            if (!string.IsNullOrWhiteSpace(updatedEntity.Password))
                existing.PasswordHash = _passwordService.HashPassword(updatedEntity.Password);

            if (!string.IsNullOrWhiteSpace(updatedEntity.FirstName))
                existing.FirstName = updatedEntity.FirstName;

            if (!string.IsNullOrWhiteSpace(updatedEntity.LastName))
                existing.LastName = updatedEntity.LastName;

            if (updatedEntity.IsActive.HasValue)
                existing.IsActive = updatedEntity.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(updatedEntity.UpdatedBy))
                existing.UpdatedBy = updatedEntity.UpdatedBy;


            //existing.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new { message = "Error al actualizar el usuario.", details = ex.Message });
            }

            var updatedDto = _mapper.Map<AppUserDto>(existing); 
            return Ok(updatedDto);
        }

        [HttpDelete("{userId:int}")]
        // [Authorize] // Removed as it's now at class level
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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (loginDto == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserName == loginDto.UserName);

            if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Credenciales inválidas." });

            var token = _tokenService.CreateToken(user);

            // Fetch user permissions to return in the response
            var userPermissions = await _context.UserPermissions
                .Where(up => up.UserId == user.UserId)
                .Include(up => up.Permission)
                .Select(up => up.Permission.PermissionKey)
                .ToListAsync();

            return Ok(new { token, username = user.UserName, permissions = userPermissions }); // Added username
        }

        [HttpGet("{userId}/sedes-areas")]
        [Authorize] // Keep this if it needs specific authorization rules beyond just being logged in
        public async Task<IActionResult> GetSedesYAreasDeUsuario(int userId)
        {
            var usuario = await _context.AppUsers.FindAsync(userId);
            if (usuario == null)
                return NotFound(new { message = "No se encontró el usuario especificado." });

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