
using Entities.Manager;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Controllers.SiteCostCenterController
{
    /// <summary>
    /// Controlador para gestionar las relaciones Sede-Centro de Costo (SiteCostCenter).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SiteCostCenterController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del DbContext.
        /// </summary>
        public SiteCostCenterController(ApplicationDbcontext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las relaciones Sede-Centro de Costo.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SiteCostCenter>>> GetAll()
        {
            var list = await _context.SiteCostCenters.ToListAsync();
            if (list == null || !list.Any())
                return NotFound(new { message = "No se encontraron relaciones Sede-Centro de Costo." });

            return Ok(list);
        }

        /// <summary>
        /// Obtiene una relación Sede-Centro de Costo por su clave compuesta.
        /// </summary>
        [HttpGet("{siteId}/{costCenterId}")]
        public async Task<ActionResult<SiteCostCenter>> GetById(string siteId, string costCenterId)
        {
            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(costCenterId))
                return BadRequest(new { message = "El ID de sede y el ID de centro de costo son obligatorios." });

            var entity = await _context.SiteCostCenters.FindAsync(siteId, costCenterId);

            if (entity == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Centro de Costo con SiteId='{siteId}' y CostCenterId='{costCenterId}'." });

            return Ok(entity);
        }

        /// <summary>
        /// Crea una nueva relación Sede-Centro de Costo.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(object))] // Ahora documenta el 201
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SiteCostCenter>> Create([FromBody] SiteCostCenter siteCostCenter)
        {
            if (siteCostCenter == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (string.IsNullOrWhiteSpace(siteCostCenter.SiteId) || string.IsNullOrWhiteSpace(siteCostCenter.CostCenterId))
                return BadRequest(new { message = "El ID de sede y el ID de centro de costo son obligatorios." });

            // Validar si ya existe la relación
            var exists = await _context.SiteCostCenters.FindAsync(siteCostCenter.SiteId, siteCostCenter.CostCenterId);
            if (exists != null)
                return Conflict(new { message = "Ya existe una relación Sede-Centro de Costo con esos identificadores." });

            // Valores por defecto
            siteCostCenter.Active ??= "Y";
            siteCostCenter.CreationDate ??= System.DateTime.Now;

            _context.SiteCostCenters.Add(siteCostCenter);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { siteId = siteCostCenter.SiteId, costCenterId = siteCostCenter.CostCenterId },
                new
                {
                    message = "Relación Sede-Centro Costo creada correctamente.",
                    data = siteCostCenter
                });
        }

        /// <summary>
        /// Actualiza una relación Sede-Centro de Costo existente.
        /// </summary>
        [HttpPut("{siteId}/{costCenterId}")]
        public async Task<IActionResult> Update(string siteId, string costCenterId, [FromBody] SiteCostCenter updatedEntity)
        {
            if (updatedEntity == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (siteId != updatedEntity.SiteId || costCenterId != updatedEntity.CostCenterId)
                return BadRequest(new { message = "Los identificadores de la ruta y el cuerpo no coinciden." });

            var existing = await _context.SiteCostCenters.FindAsync(siteId, costCenterId);
            if (existing == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Centro de Costo con SiteId='{siteId}' y CostCenterId='{costCenterId}'." });

            // Actualizar propiedades
            existing.Observation = updatedEntity.Observation;
            existing.SiteName = updatedEntity.SiteName;
            existing.CostCenterName = updatedEntity.CostCenterName;
            existing.CreatedBy = updatedEntity.CreatedBy;
            existing.CreationDate = updatedEntity.CreationDate;
            existing.Active = updatedEntity.Active;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la relación Sede-Centro de Costo.", details = ex.Message });
            }

            return Ok(existing);
        }

        /// <summary>
        /// Elimina una relación Sede-Centro de Costo por su clave compuesta.
        /// </summary>
        [HttpDelete("{siteId}/{costCenterId}")]
        public async Task<IActionResult> Delete(string siteId, string costCenterId)
        {
            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(costCenterId))
                return BadRequest(new { message = "El ID de sede y el ID de centro de costo son obligatorios." });

            var entity = await _context.SiteCostCenters.FindAsync(siteId, costCenterId);
            if (entity == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Centro de Costo con SiteId='{siteId}' y CostCenterId='{costCenterId}'." });

            _context.SiteCostCenters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Relación Sede-Centro de Costo eliminada correctamente." });
        }
    }
}
