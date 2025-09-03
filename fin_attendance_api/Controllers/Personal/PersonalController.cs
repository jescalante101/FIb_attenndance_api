using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Dtos.Personal;
using FibAttendanceApi.Data;
using Entities.Personal;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace FibAttendanceApi.Controllers.Personal
{
    /// <summary>
    /// Controller for managing Personal records
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PersonalController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;
        private readonly ILogger<PersonalController> _logger;
        private readonly IMapper _mapper;

        public PersonalController(ApplicationDbcontext context, ILogger<PersonalController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all personal records
        /// </summary>
        /// <returns>List of personal records</returns>
        /// <response code="200">Returns the list of personal records</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PersonalDto>>> GetAllPersonal()
        {
            try
            {
                _logger.LogInformation("Getting all personal records");

                var personalList = await _context.Personals
                 // ProjectTo es la magia. Traduce el mapeo a una consulta SQL SELECT
                 // para que Entity Framework solo traiga los campos necesarios.
                 .ProjectTo<PersonalDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();

                   return Ok(personalList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all personal records");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets a specific personal record by its primary key ID
        /// </summary>
        /// <param name="id">The numeric primary key of the personal record</param>
        /// <returns>Personal record</returns>
        [HttpGet("{id:int}", Name = "GetPersonal")] // Agregamos un nombre a la ruta para que CreateAtAction lo encuentre
        [ProducesResponseType(typeof(PersonalDto), StatusCodes.Status200OK)]
        // ... (los otros ProducesResponseType se mantienen igual)
        public async Task<ActionResult<PersonalDto>> GetPersonal(int id)
        {
            try
            {
                _logger.LogInformation("Getting personal record with DB ID: {Id}", id);

                // La búsqueda ahora es por el ID numérico y es más eficiente
                var personal = await _context.Personals
                    .Where(p => p.Id == id)
                    .ProjectTo<PersonalDto>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();

                if (personal == null)
                {
                    _logger.LogWarning("Personal record with DB ID: {Id} not found", id);
                    return NotFound($"Personal with ID '{id}' not found");
                }

                return Ok(personal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting personal record with DB ID: {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Creates a new personal record
        /// </summary>
        /// <param name="createPersonalDto">Personal data to create</param>
        /// <returns>Created personal record</returns>
        /// <response code="201">Personal record created successfully</response>
        /// <response code="400">Bad request - validation errors</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="409">Conflict - Personal ID already exists</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(PersonalDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PersonalDto>> CreatePersonal([FromBody] CreatePersonalDto createPersonalDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Validating new personal record for PersonalId: {PersonalId}", createPersonalDto.PersonalId);

                // 1. LÓGICA DE NEGOCIO: Se mantiene intacta.
                // Verificamos si ya existe un registro con fechas que se cruzan.
                var existingPersonal = await _context.Personals
                    .AnyAsync(p => p.PersonalId == createPersonalDto.PersonalId &&
                                   createPersonalDto.StartDate < (p.EndDate ?? DateTime.MaxValue) &&
                                   (createPersonalDto.EndDate ?? DateTime.MaxValue) > p.StartDate);

                if (existingPersonal)
                {
                    _logger.LogWarning("A record for PersonalId: {PersonalId} with an overlapping date range already exists", createPersonalDto.PersonalId);
                    return Conflict($"A record for Personal ID '{createPersonalDto.PersonalId}' with an overlapping date range already exists");
                }

                // 2. MAPEO: Usamos AutoMapper para crear la entidad a partir del DTO.
                var personalEntity = _mapper.Map<PersonalEntity>(createPersonalDto);

                // 3. LÓGICA DE SEGURIDAD Y AUDITORÍA: Se aplica DESPUÉS del mapeo.
                // Sobreescribimos 'CreatedBy' con el usuario autenticado para mayor seguridad.
                var currentUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                personalEntity.CreatedBy = currentUser;

                // Nota: CreatedAt y ApprovalStatus son asignados por el constructor de la entidad.

                // 4. GUARDADO EN DB: Se mantiene igual.
                _context.Personals.Add(personalEntity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Personal record created successfully with new DB ID: {Id}", personalEntity.Id);

                // 5. MAPEO DE RESPUESTA: Mapeamos la entidad (ya con su Id de DB) de vuelta a un DTO.
                var createdPersonalDto = _mapper.Map<PersonalDto>(personalEntity);

                return CreatedAtAction("GetPersonalById", new { id = createdPersonalDto.Id }, createdPersonalDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating personal record");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }



        /// <summary>
        /// Creates multiple personal records in a single batch.
        /// </summary>
        /// <param name="massiveCreateDto">A list of personal data to create.</param>
        /// <returns>A list of the created personal records.</returns>
        /// <response code="200">Personal records created successfully.</response>
        /// <response code="400">Bad request - The list is empty or contains invalid data.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="409">Conflict - The batch contains overlapping date ranges or conflicts with existing data.</response>
        [HttpPost("personal-transfers/massive")]
        [ProducesResponseType(typeof(List<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PersonalDto>>> CreateMassivePersonal([FromBody] List<CreatePersonalDto> massiveCreateDto)
        {
            // 1. VALIDACIÓN INICIAL: Se mantiene igual.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (massiveCreateDto == null || !massiveCreateDto.Any())
            {
                return BadRequest("The list of personal records cannot be empty.");
            }

            _logger.LogInformation("Starting massive creation process for {Count} records.", massiveCreateDto.Count);

            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "12";
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "sistema";

            try
            {
                // 2. LÓGICA DE NEGOCIO (VALIDACIÓN DE CONFLICTOS): Se mantiene intacta.
                // Es la parte más importante y no debe ser alterada.

                // a) Validar conflictos DENTRO de la lista enviada
                var groups = massiveCreateDto.GroupBy(p => p.PersonalId).ToList();
                foreach (var group in groups)
                {
                    var records = group.ToList();
                    for (int i = 0; i < records.Count; i++)
                    {
                        for (int j = i + 1; j < records.Count; j++)
                        {
                            bool overlaps = records[i].StartDate < (records[j].EndDate ?? DateTime.MaxValue) &&
                                            (records[i].EndDate ?? DateTime.MaxValue) > records[j].StartDate;

                            if (overlaps)
                            {
                                var errorMessage = $"The provided list contains overlapping date ranges for PersonalId '{group.Key}'.";
                                _logger.LogWarning(errorMessage);
                                return Conflict(errorMessage);
                            }
                        }
                    }
                }

                // b) Validar conflictos de la lista enviada CONTRA la base de datos
                var personalIdsInRequest = massiveCreateDto.Select(p => p.PersonalId).Distinct().ToList();
                var existingRecords = await _context.Personals
                    .Where(p => personalIdsInRequest.Contains(p.PersonalId))
                    .ToListAsync();

                foreach (var dto in massiveCreateDto)
                {
                    var conflict = existingRecords
                        .Any(p => p.PersonalId == dto.PersonalId &&
                                  dto.StartDate < (p.EndDate ?? DateTime.MaxValue) &&
                                  (dto.EndDate ?? DateTime.MaxValue) > p.StartDate);

                    if (conflict)
                    {
                        var errorMessage = $"A record for Personal ID '{dto.PersonalId}' with an overlapping date range already exists in the database.";
                        _logger.LogWarning(errorMessage);
                        return Conflict(errorMessage);
                    }
                }

                // 3. MAPEO DE ENTIDADES: Reemplazamos el bucle manual con una sola llamada a AutoMapper.
                var newEntities = _mapper.Map<List<PersonalEntity>>(massiveCreateDto);

                // 4. LÓGICA DE AUDITORÍA: Aplicamos el usuario actual a todas las nuevas entidades.
                // Esto se hace DESPUÉS del mapeo para garantizar la seguridad.
                newEntities.ForEach(entity =>
                {
                    // Asignamos el nombre/ID de usuario en formato de texto
                    entity.CreatedBy = username;

                    // Intentamos convertir el ID a número de forma segura
                    if (int.TryParse(currentUser, out int userId))
                    {
                        // Si la conversión es exitosa, asignamos el ID numérico
                        entity.CreatedById = userId;
                    }
                    // Opcional: else { entity.CreatedById = null; } si la conversión falla
                });

                // 5. GUARDADO EN BASE DE DATOS: Se mantiene igual, usando AddRange para eficiencia.
                _context.Personals.AddRange(newEntities);
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Count} personal records created successfully.", newEntities.Count);

                // 6. MAPEO DE RESPUESTA: Reemplazamos el .Select() manual con AutoMapper.
                var resultDtos = _mapper.Map<List<PersonalDto>>(newEntities);

                return Ok(resultDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the massive personal creation process.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates an existing personal record
        /// </summary>
        /// <param name="id">The numeric ID of the personal record</param>
        /// <param name="updatePersonalDto">Personal data to update</param>
        /// <returns>Updated personal record</returns>
        /// <response code="200">Personal record updated successfully</response>
        /// <response code="400">Bad request - validation errors</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Personal record not found</response>
        /// <response code="409">Conflict - The update would cause an overlapping date range</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PersonalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // ... otros atributos
        public async Task<ActionResult<PersonalDto>> UpdatePersonal(int id, [FromBody] UpdatePersonalDto updatePersonalDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating personal record with DB ID: {Id}", id);

                // 1. BÚSQUEDA Y VALIDACIÓN DE NEGOCIO: Se mantienen intactas.
                var personalEntity = await _context.Personals.FindAsync(id);

                if (personalEntity == null)
                {
                    _logger.LogWarning("Personal record with DB ID: {Id} not found for update", id);
                    return NotFound($"Personal record with ID '{id}' not found");
                }

                // Validar conflicto de fechas antes de actualizar (se mantiene tu lógica robusta)
                var newStartDate = updatePersonalDto.StartDate ?? personalEntity.StartDate;
                var newEndDate = updatePersonalDto.EndDate;
                var conflictExists = await _context.Personals
                    .AnyAsync(p => p.PersonalId == personalEntity.PersonalId &&
                                   p.Id != id &&
                                   newStartDate < (p.EndDate ?? DateTime.MaxValue) &&
                                   (newEndDate ?? DateTime.MaxValue) > p.StartDate);

                if (conflictExists)
                {
                    var errorMessage = $"The update for Personal ID '{personalEntity.PersonalId}' creates an overlapping date range with another existing record.";
                    _logger.LogWarning(errorMessage);
                    return Conflict(errorMessage);
                }

                // 2. MAPEO: Usamos AutoMapper para aplicar los cambios del DTO a la entidad existente.
                _mapper.Map(updatePersonalDto, personalEntity);

                // 3. LÓGICA DE AUDITORÍA: Aplicamos el usuario actual DESPUÉS del mapeo.
               // var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "12";
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                personalEntity.UpdatedBy = username;

                // 4. GUARDADO EN DB: Se mantiene igual.
                await _context.SaveChangesAsync();
                _logger.LogInformation("Personal record with DB ID: {Id} updated successfully", id);

                // 5. MAPEO DE RESPUESTA: Mapeamos la entidad actualizada de vuelta a un DTO.
                // Esto es más eficiente, ya que evita una segunda consulta a la base de datos.
                var updatedPersonalDto = _mapper.Map<PersonalDto>(personalEntity);

                return Ok(updatedPersonalDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating personal record with DB ID: {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Deletes a personal record
        /// </summary>
        /// <param name="id">Personal ID</param>
        /// <returns>No content</returns>
        /// <response code="204">Personal record deleted successfully</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="404">Personal record not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePersonal(int id)
        {
            try
            {
                _logger.LogInformation("Deleting personal record with ID: {PersonalId}", id);

                var personal = await _context.Personals.FindAsync(id);

                if (personal == null)
                {
                    _logger.LogWarning("Personal record with ID: {PersonalId} not found for deletion", id);
                    return NotFound($"Personal with ID '{id}' not found");
                }

                _context.Personals.Remove(personal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Personal record deleted successfully with ID: {PersonalId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting personal record with ID: {PersonalId}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets personal records by branch ID
        /// </summary>
        /// <param name="branchId">Branch ID</param>
        /// <returns>List of personal records for the specified branch</returns>
        /// <response code="200">Returns the list of personal records</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("branch/{branchId}")]
        [ProducesResponseType(typeof(IEnumerable<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PersonalDto>>> GetPersonalByBranch(string branchId)
        {
            try
            {
                _logger.LogInformation("Getting personal records for branch ID: {BranchId}", branchId);

                var personalList = await _context.Personals
                    .Where(p => p.BranchId == branchId)
                    .Select(p => new PersonalDto
                    {
                        Id = p.Id,
                        PersonalId = p.PersonalId,
                        FullName = p.FullName,
                        BranchId = p.BranchId,
                        BranchDescription = p.BranchDescription,
                        AreaId = p.AreaId,
                        AreaDescription = p.AreaDescription,
                        CostCenterId = p.CostCenterId,
                        CostCenterDescription = p.CostCenterDescription,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Observation = p.Observation,
                        CreatedBy = p.CreatedBy,
                        CreatedAt = p.CreatedAt,
                        UpdatedBy = p.UpdatedBy,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(personalList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting personal records for branch ID: {BranchId}", branchId);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets personal records by area ID
        /// </summary>
        /// <param name="areaId">Area ID</param>
        /// <returns>List of personal records for the specified area</returns>
        /// <response code="200">Returns the list of personal records</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("area/{areaId}")]
        [ProducesResponseType(typeof(IEnumerable<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PersonalDto>>> GetPersonalByArea(string areaId)
        {
            try
            {
                _logger.LogInformation("Getting personal records for area ID: {AreaId}", areaId);

                var personalList = await _context.Personals
                    .Where(p => p.AreaId == areaId)
                    .Select(p => new PersonalDto
                    {
                        Id = p.Id,
                        PersonalId = p.PersonalId,
                        FullName = p.FullName,
                        BranchId = p.BranchId,
                        BranchDescription = p.BranchDescription,
                        AreaId = p.AreaId,
                        AreaDescription = p.AreaDescription,
                        CostCenterId = p.CostCenterId,
                        CostCenterDescription = p.CostCenterDescription,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Observation = p.Observation,
                        CreatedBy = p.CreatedBy,
                        CreatedAt = p.CreatedAt,
                        UpdatedBy = p.UpdatedBy,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(personalList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting personal records for area ID: {AreaId}", areaId);
                return StatusCode(500, "An error occurred while processing your request");
            }
      }
    

    /// <summary>
/// Gets personal records with pagination
/// </summary>
/// <param name="pageNumber">Page number (default: 1)</param>
/// <param name="pageSize">Page size (default: 10, max: 100)</param>
/// <param name="searchTerm">Optional search term to filter by name or ID</param>
/// <param name="branchId">Optional filter by branch ID</param>
/// <param name="areaId">Optional filter by area ID</param>
/// <param name="costCenterId">Optional filter by cost center ID</param>
/// <param name="isActive">Optional filter by active status (null for all, true for active, false for inactive)</param>
/// <returns>Paginated list of personal records</returns>
/// <response code="200">Returns the paginated list of personal records</response>
/// <response code="400">Bad request - invalid parameters</response>
/// <response code="401">Unauthorized access</response>
/// <response code="500">Internal server error</response>
[HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponse<PersonalDto>>> GetPersonalPaginated(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? searchTerm = null,
    [FromQuery] string? branchId = null,
    [FromQuery] string? areaId = null,
    [FromQuery] string? costCenterId = null,
    [FromQuery] bool? isActive = null)
        {
            try
            {
                // Validate parameters
                if (pageNumber < 1)
                {
                    return BadRequest("Page number must be greater than 0");
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100");
                }

                _logger.LogInformation("Getting paginated personal records - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
                    pageNumber, pageSize, searchTerm);

                // Start with base query
                var query = _context.Personals.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.PersonalId.ToLower().Contains(searchTermLower) ||
                        (p.FullName != null && p.FullName.ToLower().Contains(searchTermLower)));
                }

                if (!string.IsNullOrWhiteSpace(branchId))
                {
                    query = query.Where(p => p.BranchId == branchId);
                }

                if (!string.IsNullOrWhiteSpace(areaId))
                {
                    query = query.Where(p => p.AreaId == areaId);
                }

                if (!string.IsNullOrWhiteSpace(costCenterId))
                {
                    query = query.Where(p => p.CostCenterId == costCenterId);
                }

                if (isActive.HasValue)
                {
                    if (isActive.Value)
                    {
                        // Active: no end date or end date is in the future
                        query = query.Where(p => p.EndDate == null || p.EndDate > DateTime.Now);
                    }
                    else
                    {
                        // Inactive: has end date and it's in the past
                        query = query.Where(p => p.EndDate != null && p.EndDate <= DateTime.Now);
                    }
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Calculate total pages
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination and sorting
                var personalList = await query
                    .OrderBy(p => p.PersonalId) // Default sorting
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PersonalDto
                    {
                        Id = p.Id,
                        PersonalId = p.PersonalId,
                        FullName = p.FullName,
                        BranchId = p.BranchId,
                        BranchDescription = p.BranchDescription,
                        AreaId = p.AreaId,
                        AreaDescription = p.AreaDescription,
                        CostCenterId = p.CostCenterId,
                        CostCenterDescription = p.CostCenterDescription,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Observation = p.Observation,
                        CreatedBy = p.CreatedBy,
                        CreatedAt = p.CreatedAt,
                        UpdatedBy = p.UpdatedBy,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                var paginatedResponse = new PaginatedResponse<PersonalDto>
                {
                    Data = personalList,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                _logger.LogInformation("Successfully retrieved {Count} personal records out of {TotalCount} total records",
                    personalList.Count, totalCount);

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paginated personal records");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Gets personal records with pagination and advanced sorting
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <param name="searchTerm">Optional search term to filter by name or ID</param>
        /// <param name="branchId">Optional filter by branch ID</param>
        /// <param name="areaId">Optional filter by area ID</param>
        /// <param name="costCenterId">Optional filter by cost center ID</param>
        /// <param name="isActive">Optional filter by active status</param>
        /// <param name="sortBy">Sort field: PersonalId, FullName, BranchId, AreaId, StartDate, CreatedAt (default: PersonalId)</param>
        /// <param name="sortDirection">Sort direction: asc or desc (default: asc)</param>
        /// <returns>Paginated and sorted list of personal records</returns>
        /// <response code="200">Returns the paginated and sorted list of personal records</response>
        /// <response code="400">Bad request - invalid parameters</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("paginated/advanced")]
        [ProducesResponseType(typeof(PaginatedResponse<PersonalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponse<PersonalDto>>> GetPersonalPaginatedAdvanced(
            [FromQuery] string companyId, // <-- AHORA ES OBLIGATORIO
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? createdById = 0, // <-- FILTRO AÑADIDO
            [FromQuery] string? branchId = null,
            [FromQuery] string? areaId = null,
            [FromQuery] string? costCenterId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? approvalStatus = null,
            [FromQuery] string sortBy = "Id",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {

                // Validate parameters
                if (pageNumber < 1)
                {
                    return BadRequest("Page number must be greater than 0");
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100");
                }

                if (string.IsNullOrEmpty(companyId))
                {
                    return BadRequest("El parámetro 'companyId' es obligatorio.");
                }


                var validSortFields = new[] { "PersonalId", "FullName", "BranchId", "AreaId", "StartDate", "CreatedAt" };
                if (!validSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest($"Invalid sort field. Valid fields are: {string.Join(", ", validSortFields)}");
                }

                if (!new[] { "asc", "desc" }.Contains(sortDirection.ToLower()))
                {
                    return BadRequest("Sort direction must be 'asc' or 'desc'");
                }

                _logger.LogInformation("Getting advanced paginated personal records - Page: {PageNumber}, Size: {PageSize}, Sort: {SortBy} {SortDirection}",
                    pageNumber, pageSize, sortBy, sortDirection);

                // Start with base query
                var query = _context.Personals.AsQueryable();

                query = query.Where(p => p.CompanyId == companyId);

                // Apply filters (same as previous method)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.PersonalId.ToLower().Contains(searchTermLower) ||
                        (p.FullName != null && p.FullName.ToLower().Contains(searchTermLower)));
                }

                if (createdById!=0)
                {
                    query = query.Where(p => p.CreatedById == createdById);
                }

                if (!string.IsNullOrWhiteSpace(branchId))
                {
                    query = query.Where(p => p.BranchId == branchId);
                }

                if (!string.IsNullOrWhiteSpace(areaId))
                {
                    query = query.Where(p => p.AreaId == areaId);
                }

                if (!string.IsNullOrWhiteSpace(costCenterId))
                {
                    query = query.Where(p => p.CostCenterId == costCenterId);
                }

                if (isActive.HasValue)
                {
                    if (isActive.Value)
                    {
                        query = query.Where(p => p.EndDate == null || p.EndDate > DateTime.Now);
                    }
                    else
                    {
                        query = query.Where(p => p.EndDate != null && p.EndDate <= DateTime.Now);
                    }
                }

                if (!string.IsNullOrWhiteSpace(approvalStatus)) query = query.Where(p => p.ApprovalStatus == approvalStatus);

                // Apply dynamic sorting
                query = sortBy.ToLower() switch
                {
                    "personalid" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.PersonalId) : query.OrderBy(p => p.PersonalId),
                    "fullname" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.FullName) : query.OrderBy(p => p.FullName),
                    "branchid" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.BranchId) : query.OrderBy(p => p.BranchId),
                    "areaid" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.AreaId) : query.OrderBy(p => p.AreaId),
                    "startdate" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
                    "createdat" => sortDirection.ToLower() == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.PersonalId) // Default fallback
                };

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Calculate total pages
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var personalList = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<PersonalDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

                var paginatedResponse = new PaginatedResponse<PersonalDto>
                {
                    Data = personalList,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                _logger.LogInformation("Successfully retrieved {Count} personal records out of {TotalCount} total records with sorting {SortBy} {SortDirection}",
                    personalList.Count, totalCount, sortBy, sortDirection);

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting advanced paginated personal records");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }


    }