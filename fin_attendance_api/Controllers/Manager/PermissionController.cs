using AutoMapper;
using Dtos.Manager;
using Entities.Manager;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Added for Authorize

namespace FibAttendanceApi.Controllers.Manager
{
    [ApiController]
    [Route("api/[controller]")] // api/Permission
    [Authorize] // Added Authorize at class level
    public class PermissionController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly IMapper _mapper;

        public PermissionController(ApplicationDbcontext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // --- Endpoints for Permissions ---

        /// <summary>
        /// Obtiene todos los permisos.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAllPermissions()
        {
            var permissions = await _context.Permissions.ToListAsync();
            if (permissions == null || !permissions.Any())
            {
                return NotFound(new { message = "No se encontraron permisos." });
            }
            var permissionDtos = _mapper.Map<IEnumerable<PermissionDto>>(permissions);
            return Ok(permissionDtos);
        }

        /// <summary>
        /// Obtiene un permiso por su ID.
        /// </summary>
        [HttpGet("{permissionId:int}")]
        public async Task<ActionResult<PermissionDto>> GetPermissionById(int permissionId)
        {
            if (permissionId <= 0)
            {
                return BadRequest(new { message = "El ID del permiso debe ser mayor que cero." });
            }

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return NotFound(new { message = $"No se encontró el permiso con ID={permissionId}." });
            }
            var permissionDto = _mapper.Map<PermissionDto>(permission);
            return Ok(permissionDto);
        }

        /// <summary>
        /// Crea un nuevo permiso.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] PermissionCreateDto permissionCreateDto)
        {
            if (permissionCreateDto == null)
            {
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });
            }

            // Check for existing permission key or name
            var exists = await _context.Permissions.AnyAsync(p =>
                p.PermissionKey == permissionCreateDto.PermissionKey ||
                p.PermissionName == permissionCreateDto.PermissionName);

            if (exists)
            {
                return Conflict(new { message = $"Ya existe un permiso con la clave '{permissionCreateDto.PermissionKey}' o el nombre '{permissionCreateDto.PermissionName}'." });
            }

            var permission = _mapper.Map<Permission>(permissionCreateDto);
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            var permissionDto = _mapper.Map<PermissionDto>(permission);
            return CreatedAtAction(nameof(GetPermissionById), new { permissionId = permission.PermissionId }, permissionDto);
        }

        /// <summary>
        /// Actualiza un permiso existente.
        /// </summary>
        [HttpPut("{permissionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePermission(int permissionId, [FromBody] PermissionUpdateDto permissionUpdateDto)
        {
            if (permissionUpdateDto == null)
            {
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });
            }

            var existingPermission = await _context.Permissions.FindAsync(permissionId);
            if (existingPermission == null)
            {
                return NotFound(new { message = $"No se encontró el permiso con ID={permissionId}." });
            }

            // Update properties only if provided in the DTO
            if (!string.IsNullOrWhiteSpace(permissionUpdateDto.PermissionKey))
            {
                // Check for key conflict if changing
                if (await _context.Permissions.AnyAsync(p => p.PermissionKey == permissionUpdateDto.PermissionKey && p.PermissionId != permissionId))
                {
                    return Conflict(new { message = $"Ya existe un permiso con la clave '{permissionUpdateDto.PermissionKey}'." });
                }
                existingPermission.PermissionKey = permissionUpdateDto.PermissionKey;
            }

            if (!string.IsNullOrWhiteSpace(permissionUpdateDto.PermissionName))
            {
                // Check for name conflict if changing
                if (await _context.Permissions.AnyAsync(p => p.PermissionName == permissionUpdateDto.PermissionName && p.PermissionId != permissionId))
                {
                    return Conflict(new { message = $"Ya existe un permiso con el nombre '{permissionUpdateDto.PermissionName}'." });
                }
                existingPermission.PermissionName = permissionUpdateDto.PermissionName;
            }

            if (permissionUpdateDto.Description != null) // Allow setting to null
            {
                existingPermission.Description = permissionUpdateDto.Description;
            }

            _context.Entry(existingPermission).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermissionExists(permissionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Permiso actualizado correctamente." });
        }

        /// <summary>
        /// Elimina un permiso por su ID.
        /// </summary>
        [HttpDelete("{permissionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePermission(int permissionId)
        {
            if (permissionId <= 0)
            {
                return BadRequest(new { message = "El ID del permiso debe ser mayor que cero." });
            }

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return NotFound(new { message = $"No se encontró el permiso con ID={permissionId}." });
            }

            // Check if any users are assigned this permission before deleting
            var hasAssignments = await _context.UserPermissions.AnyAsync(up => up.PermissionId == permissionId);
            if (hasAssignments)
            {
                return Conflict(new { message = "No se puede eliminar el permiso porque está asignado a uno o más usuarios." });
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permiso eliminado correctamente." });
        }

        // --- Endpoints for User Permissions ---

        /// <summary>
        /// Obtiene todos los permisos asignados a un usuario específico.
        /// </summary>
        [HttpGet("/api/users/{userId:int}/permissions")]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserPermissions(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "El ID de usuario debe ser mayor que cero." });
            }

            var userPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission) // Include Permission details
                .Include(up => up.User) // Include User details
                .Select(up => new UserPermissionDto
                {
                    UserId = up.UserId,
                    PermissionId = up.PermissionId,
                    UserName = up.User.UserName,
                    PermissionKey = up.Permission.PermissionKey,
                    PermissionName = up.Permission.PermissionName
                })
                .ToListAsync();

            if (userPermissions == null || !userPermissions.Any())
            {
                return NotFound(new { message = $"No se encontraron permisos para el usuario con ID={userId}." });
            }

            return Ok(userPermissions);
        }

        /// <summary>
        /// Asigna un permiso a un usuario.
        /// </summary>
        [HttpPost("/api/users/{userId:int}/permissions/{permissionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignPermissionToUser(int userId, int permissionId)
        {
            if (userId <= 0 || permissionId <= 0)
            {
                return BadRequest(new { message = "Los IDs de usuario y permiso deben ser mayores que cero." });
            }

            var userExists = await _context.AppUsers.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                return NotFound(new { message = $"No se encontró el usuario con ID={userId}." });
            }

            var permissionExists = await _context.Permissions.AnyAsync(p => p.PermissionId == permissionId);
            if (!permissionExists)
            {
                return NotFound(new { message = $"No se encontró el permiso con ID={permissionId}." });
            }

            var existingAssignment = await _context.UserPermissions.FindAsync(userId, permissionId);
            if (existingAssignment != null)
            {
                return Conflict(new { message = $"El permiso con ID={permissionId} ya está asignado al usuario con ID={userId}." });
            }

            var userPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId
            };

            _context.UserPermissions.Add(userPermission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permiso asignado correctamente al usuario." });
        }

        /// <summary>
        /// Revoca un permiso de un usuario.
        /// </summary>
        [HttpDelete("/api/users/{userId:int}/permissions/{permissionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokePermissionFromUser(int userId, int permissionId)
        {
            if (userId <= 0 || permissionId <= 0)
            {
                return BadRequest(new { message = "Los IDs de usuario y permiso deben ser mayores que cero." });
            }

            var userPermission = await _context.UserPermissions.FindAsync(userId, permissionId);
            if (userPermission == null)
            {
                return NotFound(new { message = $"No se encontró la asignación del permiso con ID={permissionId} para el usuario con ID={userId}." });
            }

            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permiso revocado correctamente del usuario." });
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.PermissionId == id);
        }
    }
}