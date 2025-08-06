
using Entities.Manager;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Controllers.Manager.SiteAreaCostCenterController
{
    /// <summary>
    /// Controlador para gestionar las relaciones Sede-Área (SiteArea).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SiteAreaCostCenterController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del DbContext.
        /// </summary>
        public SiteAreaCostCenterController(ApplicationDbcontext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las relaciones Sede-Área.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SiteAreaCostCenter>>> GetAll()
        {
            var list = await _context.SiteAreaCostCenters.ToListAsync();
            if (list == null || !list.Any())
                return NotFound(new { message = "No se encontraron relaciones Sede-Área." });

            return Ok(list);
        }

        /// <summary>
        /// Obtiene una relación Sede-Área por su clave compuesta.
        /// </summary>
        [HttpGet("{siteId}/{areaId}")]
        public async Task<ActionResult<SiteAreaCostCenter>> GetById(string siteId, string areaId)
        {
            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(areaId))
                return BadRequest(new { message = "El ID de sede y el ID de área son obligatorios." });

            var entity = await _context.SiteAreaCostCenters.FindAsync(siteId, areaId);

            if (entity == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Área con SiteId='{siteId}' y AreaId='{areaId}'." });

            return Ok(entity);
        }

        /// <summary>
        /// Crea una nueva relación Sede-Área.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(object))] // Ahora documenta el 201
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SiteAreaCostCenter>> Create([FromBody] SiteAreaCostCenter siteArea)
        {
            if (siteArea == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (string.IsNullOrWhiteSpace(siteArea.SiteId) || string.IsNullOrWhiteSpace(siteArea.AreaId))
                return BadRequest(new { message = "El ID de sede y el ID de área son obligatorios." });

            var exists = await _context.SiteAreaCostCenters.FindAsync(siteArea.SiteId, siteArea.AreaId);
            if (exists != null)
                return Conflict(new { message = "Ya existe una relación Sede-Área con esos identificadores." });

            siteArea.Active ??= "Y";
            siteArea.CreationDate ??= DateTime.Now;

            _context.SiteAreaCostCenters.Add(siteArea);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { siteId = siteArea.SiteId, areaId = siteArea.AreaId },
                new
                {
                    message = "Relación Sede-Área creada correctamente.",
                    data = siteArea
                });
        }

        /// <summary>
        /// Actualiza una relación Sede-Área existente.
        /// </summary>
        [HttpPut("{siteId}/{areaId}")]
        public async Task<IActionResult> Update(string siteId, string areaId, [FromBody] SiteAreaCostCenter updatedEntity)
        {
            if (updatedEntity == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío." });

            if (siteId != updatedEntity.SiteId || areaId != updatedEntity.AreaId)
                return BadRequest(new { message = "Los identificadores de la ruta y el cuerpo no coinciden." });

            var existing = await _context.SiteAreaCostCenters.FindAsync(siteId, areaId);
            if (existing == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Área con SiteId='{siteId}' y AreaId='{areaId}'." });

            // Actualizar propiedades
            existing.Observation = updatedEntity.Observation;
            existing.SiteId = updatedEntity.SiteId;
            existing.SiteName = updatedEntity.SiteName;
            existing.CostCenterId= updatedEntity.CostCenterId;
            existing.CostCenterName = updatedEntity.CostCenterName;
            existing.AreaId = updatedEntity.AreaId;
            existing.AreaName = updatedEntity.AreaName;
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
                return StatusCode(500, new { message = "Error al actualizar la relación Sede-Área.", details = ex.Message });
            }

            return Ok(existing);
        }

        /// <summary>
        /// Elimina una relación Sede-Área por su clave compuesta.
        /// </summary>
        [HttpDelete("{siteId}/{areaId}")]
        public async Task<IActionResult> Delete(string siteId, string areaId)
        {
            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(areaId))
                return BadRequest(new { message = "El ID de sede y el ID de área son obligatorios." });

            var entity = await _context.SiteAreaCostCenters.FindAsync(siteId, areaId);
            if (entity == null)
                return NotFound(new { message = $"No se encontró la relación Sede-Área con SiteId='{siteId}' y AreaId='{areaId}'." });

            _context.SiteAreaCostCenters.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Relación Sede-Área eliminada correctamente." });
        }
    }
}
