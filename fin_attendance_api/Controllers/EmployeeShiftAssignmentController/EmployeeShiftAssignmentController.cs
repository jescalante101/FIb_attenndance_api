using DocumentFormat.OpenXml.InkML;
using Dtos.EmployeeShiftAssignmentDto;
using Dtos.ResponseDto;
using Dtos.ShiftDto;
using Entities.Manager;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Linq;

namespace FibAttendanceApi.Controllers.EmployeeShiftAssignmentController
{
    [ApiController]
    [Route("api/employee-schedule-assignment")]
    [Authorize]
    public class EmployeeShiftAssignmentController : ControllerBase
    {
        private readonly ApplicationDbcontext _db;
        public EmployeeShiftAssignmentController(ApplicationDbcontext context)
        {
            _db = context;
        }



        [HttpGet("search")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> Search(
              [FromQuery] string searchText = "",
              [FromQuery] int pageNumber = 1,
              [FromQuery] int pageSize = 15,
              [FromQuery] DateTime? startDate = null,
              [FromQuery] DateTime? endDate = null,
              [FromQuery] string? locationId = null,
              [FromQuery] string? areaId=null


          )
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
               // if (pageSize < 1 || pageSize > 100) pageSize = 15;

                var query = _db.EmployeeScheduleAssignments.AsQueryable();

                if (!string.IsNullOrWhiteSpace(locationId))
                {
                    var locations = locationId?.Split(',').Select(x => x.Trim()).ToList();

                    if (locations != null && locations.Any())
                    {
                        query = query.Where(x => locations.Contains(x.LocationId));
                    }
                }

                if (!string.IsNullOrWhiteSpace(areaId))
                {
                    query = query.Where(x => x.AreaId == areaId);
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchLower = searchText.ToLower();
                    query = query.Where(x =>
                        x.FullNameEmployee.ToLower().Contains(searchLower) ||
                        x.ShiftDescription.ToLower().Contains(searchLower) ||
                        // == NUEVOS CAMPOS EN EL FILTRO DE BÚSQUEDA ==
                        (x.CcostDescription != null && x.CcostDescription.ToLower().Contains(searchLower)) ||
                        (x.CompaniaId != null && x.CompaniaId.ToLower().Contains(searchLower))
                    );
                }
               

                if (startDate.HasValue)
                {
                    var startDateOnly = startDate.Value.Date;
                    query = query.Where(x => x.StartDate >= startDateOnly);
                }

                if (endDate.HasValue)
                {
                    var endDateOnly = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.EndDate <= endDateOnly);
                }

                
              

                query = query.OrderByDescending(x => x.CreatedAt);

                var totalCount = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new EmployeeShiftAssignmentDTO
                    {
                        AssignmentId = x.AssignmentId,
                        EmployeeId = x.EmployeeId,
                        FullNameEmployee = x.FullNameEmployee,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        Remarks = x.Remarks,
                        ScheduleName = x.ShiftDescription,
                        ScheduleId = x.ShiftId,
                        NroDoc = x.NroDoc ?? "-",
                        AreaId = x.AreaId,
                        AreaName = x.AreaDescription ?? "-",
                        CreatedAt = x.CreatedAt,
                        CreatedWeek = ISOWeek.GetWeekOfYear(x.CreatedAt),
                        LocationId = x.LocationId,
                        LocationName = x.LocationName ?? "-",
                        UpdatedAt = x.UpdatedAt,
                        CreatedBy = x.CreatedBy,
                        UpdatedBy = x.UpdatedBy,
                        // == NUEVOS CAMPOS EN LA RESPUESTA ==
                        CcostId = x.CcostId,
                        CcostDescription = x.CcostDescription,
                        CompaniaId = x.CompaniaId
                    })
                    .ToListAsync();

                var paginated = new PaginatedList<EmployeeShiftAssignmentDTO>(items, totalCount, pageNumber, pageSize);

                var result = new ResultadoConsulta<EmployeeShiftAssignmentDTO>
                {
                    Exito = true,
                    Mensaje = totalCount > 0 ? $"Se encontraron {totalCount} registros" : "No se encontraron registros",
                    Data = paginated
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorResult = new ResultadoConsulta<EmployeeShiftAssignmentDTO>
                {
                    Exito = false,
                    Mensaje = "Error interno del servidor al procesar la búsqueda",
                    Data = new PaginatedList<EmployeeShiftAssignmentDTO>(new List<EmployeeShiftAssignmentDTO>(), 0, pageNumber, pageSize)
                };
                return StatusCode(500, errorResult);
            }
        }

        // GET: api/employee-schedule-assignment/get-by-id/{id}
        [HttpGet("get-by-id/{id}")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> GetAssignmentById(int id)
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            try
            {
                var query = from a in _db.EmployeeScheduleAssignments
                            where a.AssignmentId == id
                            select new EmployeeShiftAssignmentDTO
                            {
                                AssignmentId = a.AssignmentId,
                                EmployeeId = a.EmployeeId,
                                FullNameEmployee = a.FullNameEmployee,
                                ScheduleName = a.ShiftDescription,
                                StartDate = a.StartDate,
                                EndDate = a.EndDate,
                                Remarks = a.Remarks,
                                CreatedAt = a.CreatedAt,
                                CreatedWeek = ISOWeek.GetWeekOfYear(a.CreatedAt)
                            };

                var assignment = await query.FirstOrDefaultAsync();

                if (assignment == null)
                {
                    resultado.Exito = false;
                    resultado.Mensaje = "Assignment not found.";
                    resultado.Data = null;
                    return NotFound(resultado);
                }

                resultado.Exito = true;
                resultado.Mensaje = "Assignment found.";
                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(
                    new List<EmployeeShiftAssignmentDTO> { assignment }, 1, 1, 1
                );
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = ex.Message;
                resultado.Data = null;
                return StatusCode(500, resultado);
            }
        }

        // POST: api/employee-schedule-assignment/insert
        [HttpPost("insert")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> InsertAssignment(
            [FromBody] List<CreateEmployeeShiftAssignmentDTO> requestModels)
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            try
            {
                if (requestModels == null || !requestModels.Any())
                {
                    resultado.Exito = false;
                    resultado.Mensaje = "El arreglo no debe estar vacío.";
                    return BadRequest(resultado);
                }

                // Aquí puedes mantener tus validaciones existentes...

                // Convertir DTOs a entidades usando el método ToEntity() que ya actualizamos
                var entities = requestModels.Select(request => request.ToEntity()).ToList();

                _db.EmployeeScheduleAssignments.AddRange(entities);
                await _db.SaveChangesAsync();

                // Mapear a DTOs de respuesta incluyendo los nuevos campos
                var dtos = entities.Select(entity => new EmployeeShiftAssignmentDTO
                {
                    AssignmentId = entity.AssignmentId,
                    EmployeeId = entity.EmployeeId,
                    FullNameEmployee = entity.FullNameEmployee,
                    ScheduleName = entity.ShiftDescription,
                    ScheduleId = entity.ShiftId,
                    StartDate = entity.StartDate,
                    EndDate = entity.EndDate,
                    Remarks = entity.Remarks,
                    CreatedAt = entity.CreatedAt,
                    NroDoc = entity.NroDoc ?? "-",
                    AreaId = entity.AreaId,
                    AreaName = entity.AreaDescription ?? "-",
                    CreatedWeek = ISOWeek.GetWeekOfYear(entity.CreatedAt),
                    LocationId = entity.LocationId,
                    LocationName = entity.LocationName ?? "-",
                    // == NUEVOS CAMPOS EN LA RESPUESTA ==
                    CcostId = entity.CcostId,
                    CcostDescription = entity.CcostDescription,
                    CompaniaId = entity.CompaniaId
                }).ToList();

                resultado.Exito = true;
                resultado.Mensaje = "Asignaciones creadas correctamente.";
                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(dtos, dtos.Count, 1, dtos.Count);
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = ex.Message;
            }
            return Ok(resultado);
        }



        // PUT: api/employee-schedule-assignment/update/
        [HttpPut("update/")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> UpdateAssignment(
            [FromBody] IEnumerable<EmployeeShiftAssignment> models) // Idealmente, usar un UpdateDTO aquí
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            var updatedAssignments = new List<EmployeeShiftAssignmentDTO>();
            var failedUpdates = new List<string>();

            if (models == null || !models.Any())
            {
                resultado.Exito = false;
                resultado.Mensaje = "No assignments provided for update.";
                return BadRequest(resultado);
            }

            foreach (var model in models)
            {
                try
                {
                    var entity = await _db.EmployeeScheduleAssignments.FindAsync(model.AssignmentId);
                    if (entity == null)
                    {
                        failedUpdates.Add($"Assignment with ID {model.AssignmentId} not found.");
                        continue;
                    }


                    // Update properties from the model
                    if (model.ShiftId!=0)
                    {
                        entity.ShiftId = model.ShiftId;
                    } 
                    
                    entity.StartDate = model.StartDate;
                    entity.EndDate = model.EndDate;
                    entity.Remarks = model.Remarks;
                    entity.UpdatedAt = DateTime.Now;
                    entity.UpdatedBy= User.Identity?.Name ?? "Sistema";

                    // == ACTUALIZACIÓN DE NUEVOS CAMPOS ==
                    if (!model.CcostId.IsNullOrEmpty())
                    {
                        entity.CcostId = model.CcostId;
                    }
                    if (!model.CcostDescription.IsNullOrEmpty())
                    {
                        entity.CcostDescription = model.CcostDescription;
                    }
                   
                    _db.EmployeeScheduleAssignments.Update(entity);

                    // Mapeo a DTO para la respuesta
                    updatedAssignments.Add(new EmployeeShiftAssignmentDTO
                    {
                        // ... todas las demás propiedades ...
                        FullNameEmployee = entity.FullNameEmployee,
                        // == NUEVOS CAMPOS EN LA RESPUESTA DE UPDATE ==
                        CcostId = entity.CcostId,
                        CcostDescription = entity.CcostDescription,
                        CompaniaId = entity.CompaniaId
                    });
                }
                catch (Exception ex)
                {
                    failedUpdates.Add($"Failed to update assignment with ID {model.AssignmentId}: {ex.Message}");
                }
            }

            try
            {
                await _db.SaveChangesAsync();
                resultado.Exito = true;
                resultado.Mensaje = $"Successfully updated {updatedAssignments.Count} assignments.";
                if (failedUpdates.Any())
                {
                    resultado.Mensaje += $" Some updates failed: {string.Join("; ", failedUpdates)}";
                }
                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(updatedAssignments, updatedAssignments.Count, 1, updatedAssignments.Count);
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = $"An unexpected error occurred: {ex.Message}";
            }

            return Ok(resultado);
        }

        // DELETE: api/employee-schedule-assignment/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> DeleteAssignment(int id)
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            try
            {
                var entity = await _db.EmployeeScheduleAssignments.FindAsync(id);
                if (entity == null)
                {
                    resultado.Exito = false;
                    resultado.Mensaje = "Assignment not found.";
                    return NotFound(resultado);
                }

                _db.EmployeeScheduleAssignments.Remove(entity);
                await _db.SaveChangesAsync();

                resultado.Exito = true;
                resultado.Mensaje = "Assignment deleted successfully.";
                resultado.Data = null;
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = ex.Message;
                resultado.Data = null;
            }
            return Ok(resultado);
        }


        // obtner por nro de documento del empleado
        [HttpGet("get-by-nrodoc/{nroDoc}")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> GetAssignmentById(string nroDoc)
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            try
            {
                var query = from a in _db.EmployeeScheduleAssignments
                            where a.NroDoc == nroDoc
                            select new EmployeeShiftAssignmentDTO
                            {
                                AssignmentId = a.AssignmentId,
                                EmployeeId = a.EmployeeId,
                                FullNameEmployee = a.FullNameEmployee,
                                StartDate = a.StartDate,
                                EndDate = a.EndDate,
                                Remarks = a.Remarks,
                                ScheduleName = a.ShiftDescription,
                                ScheduleId = a.ShiftId,
                                NroDoc = a.NroDoc ?? "-",
                                AreaId = a.AreaId,
                                AreaName = a.AreaDescription ?? "-",
                                CreatedAt = a.CreatedAt,
                                CreatedWeek = ISOWeek.GetWeekOfYear(a.CreatedAt),
                                LocationId = a.LocationId,
                                LocationName = a.LocationName ?? "-"
                            };

                var assignment = await query.FirstOrDefaultAsync();

                if (assignment == null)
                {
                    resultado.Exito = false;
                    resultado.Mensaje = "Assignment not found.";
                    resultado.Data = null;
                    return NotFound(resultado);
                }

                resultado.Exito = true;
                resultado.Mensaje = "Assignment found.";
                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(
                    new List<EmployeeShiftAssignmentDTO> { assignment }, 1, 1, 1
                );
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = ex.Message;
                resultado.Data = null;
                return StatusCode(500, resultado);
            }
        }

        //obtnermos horarios:
        [HttpGet("get-horaio-by-doc/{nroDoc}")]
        public IActionResult GetEmployeeSchedules(string nroDoc)
        {
            var result = (from e in _db.EmployeeScheduleAssignments
                          join a in _db.AttAttshifts on e.ShiftId equals a.Id
                          join de in _db.AttShiftdetails on a.Id equals de.ShiftId
                          where e.NroDoc == nroDoc
                          orderby a.Id, de.InTime
                          select new EmployeeShiftDto
                          {
                              Id = a.Id,
                              FullNameEmployee = e.FullNameEmployee,
                              Alias = a.Alias,
                              InTime = de.InTime.ToString("HH:mm"),
                              OutTime = de.InTime.ToLocalTime().AddMinutes(de.TimeInterval.WorkTimeDuration).ToString("HH:mm")
                          })
                          .Distinct()
                          .ToList();

            if (result == null || !result.Any())
            {
                return NotFound();  // Si no se encuentran resultados, se retorna un 404
            }

            return Ok(result);  // Si se encuentran resultados, se retorna un 200 OK con los datos
        }


        //ontner empleado como 
        [HttpGet("get-assignment-with-shift")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftArea>>> GetAssignmentWithShift(
            [FromQuery] string? nroDoc = null,
            [FromQuery] string? fullName = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? areaId = null
            )
        {
            // Validar parámetros de paginación
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 15;

            // 1. Trae todos los empleados y turnos en memoria
            var empleadosTurnosQuery = from e in _db.EmployeeScheduleAssignments
                                       join a in _db.AttAttshifts on e.ShiftId equals a.Id
                                       select new
                                       {
                                           e.FullNameEmployee,
                                           a.Id,
                                           e.NroDoc,
                                           a.Alias,
                                           e.AreaId,
                                           e.AreaDescription
                                       };

            // Filtro por nroDoc si se proporciona
            if (!string.IsNullOrWhiteSpace(nroDoc))
            {
                empleadosTurnosQuery = empleadosTurnosQuery.Where(x => x.NroDoc == nroDoc);
            }

            // Filtro por fullName si se proporciona (búsqueda parcial, case-insensitive)
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var fullNameLower = fullName.ToLower();
                empleadosTurnosQuery = empleadosTurnosQuery.Where(x => x.FullNameEmployee != null && x.FullNameEmployee.ToLower().Contains(fullNameLower));
            }
            if (!string.IsNullOrWhiteSpace(areaId))
            {
                empleadosTurnosQuery = empleadosTurnosQuery.Where(x => x.AreaId == areaId);
            }

            var totalCount = empleadosTurnosQuery.Count();

            var empleadosTurnos = empleadosTurnosQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList(); // Materializa aquí

            // 2. Trae todos los shiftdetails y sus TimeInterval en memoria
            var shiftdetails = _db.AttShiftdetails
                .Include(d => d.TimeInterval)
                .ToList(); // Materializa aquí

            // 3. Recorrer y construir resultado SIN group by
            var result = empleadosTurnos.Select(b => new EmployeeShiftArea
            {
                Id = b.Id,
                FullNameEmployee = b.FullNameEmployee,
                NroDoc = b.NroDoc,
                Alias = b.Alias,
                AreaId = b.AreaId,
                AreaName = b.AreaDescription ?? "-",
                Horarios = shiftdetails
                    .Where(d => d.ShiftId == b.Id)
                    .Select(d => new HorarioT
                    {
                        NameHora = d.TimeInterval.Alias,
                        InTime = d.TimeInterval.InTime.ToString("HH:mm"),
                        OutTime = d.TimeInterval.InTime
                                    .AddMinutes(d.TimeInterval.WorkTimeDuration)
                                    .ToString("HH:mm")
                    })
                    .ToList()
            }).ToList();

            var resultado = new ResultadoConsulta<EmployeeShiftArea>();

            if (result == null || !result.Any())
            {
                resultado.Exito = false;
                resultado.Mensaje = "No se encontraron registros.";
                resultado.Data = new PaginatedList<EmployeeShiftArea>(new List<EmployeeShiftArea>(), 0, pageNumber, pageSize);
                return NotFound(resultado);
            }

            resultado.Exito = true;
            resultado.Mensaje = $"Se encontraron {totalCount} registros.";
            resultado.Data = new PaginatedList<EmployeeShiftArea>(result, totalCount, pageNumber, pageSize);
            return Ok(resultado);
        }


        [HttpGet("idsPorRangoDeFechas")]
        public async Task<ActionResult<List<string>>> GetEmployeeIdsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate
        )
                {
            try
            {
                // Ajustamos la fecha de fin para que incluya todo el día.
                var endDateOnly = endDate.Date.AddDays(1).AddTicks(-1);
                var startDateOnly = startDate.Date;

                // La consulta se enfoca solo en obtener los IDs únicos.
                var employeeIds = await _db.EmployeeScheduleAssignments
                    // 1. Aplicamos el mismo filtro de solapamiento de fechas.
                    .Where(x => x.StartDate <= endDateOnly && x.EndDate >= startDateOnly)
                    // 2. Seleccionamos únicamente la columna del ID del empleado.
                    .Select(x => x.EmployeeId)
                    // 3. ¡Muy importante! Eliminamos duplicados para tener una lista de empleados únicos.
                    .Distinct()
                    // 4. Ejecutamos la consulta en la base de datos.
                    .ToListAsync();

                return Ok(employeeIds);
            }
            catch (Exception ex)
            {
                // Opcional: Registrar el error.
                // _logger.LogError(ex, "Error al obtener IDs de empleados por rango de fechas.");
                return StatusCode(500, "Error interno del servidor al procesar la solicitud.");
            }
        }




        /// <summary>
        /// Obtiene el horario de la semana actual para un empleado específico.
        /// </summary>
        /// <param name="employeeId">El ID del empleado (string).</param>
        [HttpGet("employee/{employeeId}/current-week-schedule")]
        [ProducesResponseType(typeof(ScheduleResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ScheduleResponseDto>> GetCurrentWeeklySchedule(string employeeId)
        {
            // 1. Definir el rango de la semana actual (Lunes a Domingo)
            var (weekStart, weekEnd) = GetCurrentWeekDateRange();

            // 2. Reutilizar la lógica del otro endpoint, pasándole el rango de la semana.
            //    Esto llama internamente a tu método GetScheduleByDateRange.
            return await GetScheduleByDateRange(employeeId, weekStart, weekEnd);
        }

        /// <summary>
        /// Obtiene el horario para un empleado dentro de un rango de fechas específico.
        /// </summary>
        /// <param name="employeeId">El ID del empleado.</param>
        /// <param name="startDate">La fecha de inicio del rango.</param>
        /// <param name="endDate">La fecha de fin del rango.</param>
        [HttpGet("employee/{employeeId}/date-range-schedule")]
        [ProducesResponseType(typeof(ScheduleResponseDto), 200)]
        [ProducesResponseType(400)] // Bad Request
        [ProducesResponseType(404)] // Not Found
        public async Task<ActionResult<ScheduleResponseDto>> GetScheduleByDateRange(
            string employeeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            // 1. Validar que el rango de fechas sea lógico
            if (startDate > endDate)
            {
                return BadRequest(new { message = "La fecha de inicio no puede ser posterior a la fecha de fin." });
            }

            // ========================= INICIO DE LA LÓGICA REFACTORIZADA =========================

            // 1. OBTENER TODAS las asignaciones que se cruzan con el rango de fechas.
            var assignments = await _db.EmployeeScheduleAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId &&
                            a.StartDate <= endDate.Date &&
                            (a.EndDate == null || a.EndDate >= startDate.Date))
                .ToListAsync();

            if (!assignments.Any())
            {
                return NotFound(new { message = $"No se encontraron asignaciones de turno para el empleado '{employeeId}' en el rango de fechas especificado." });
            }

            // 2. OBTENER TODOS los turnos necesarios de una sola vez para eficiencia.
            var shiftIds = assignments.Select(a => a.ShiftId).Distinct().ToList();
            var shifts = await GetShiftsWithBaseScheduleAsync(shiftIds);

            // 3. OBTENER TODAS las excepciones de una sola vez.
            var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();
            var exceptions = await GetScheduleExceptionsAsync(assignmentIds, startDate, endDate);

            // 4. CONSTRUIR el horario final con la nueva lógica día por día.
            var finalSchedule = BuildFinalSchedule(assignments, shifts, exceptions, startDate, endDate);

            // Usamos la información de la primera asignación para la respuesta general
            var firstAssignment = assignments.First();
            var firstShift = shifts[firstAssignment.ShiftId];

            var response = new ScheduleResponseDto
            {
                EmployeeId = firstAssignment.EmployeeId,
                FullNameEmployee = firstAssignment.FullNameEmployee,
                AssignmentId = firstAssignment.AssignmentId,
                ShiftInfo = new ShiftDto
                {
                    Id = firstShift.Id,
                    Alias = firstShift.Alias,
                    ShiftCycle = firstShift.ShiftCycle
                },
                QueryRange = new DateRangeDto { StartDate = startDate, EndDate = endDate },
                Schedule = finalSchedule
            };

            // ========================== FIN DE LA LÓGICA REFACTORIZADA ===========================


            return Ok(response);
        }

        // ====================================================================================
        // PARTE 3: MÉTODOS PRIVADOS AUXILIARES
        // ====================================================================================
        // Ahora acepta una lista de IDs y devuelve un diccionario para búsqueda rápida
        private async Task<Dictionary<int, (int Id, string Alias, int ShiftCycle, List<ShiftBaseScheduleDto> BaseSchedule)>> GetShiftsWithBaseScheduleAsync(List<int> shiftIds)
        {
            var shiftEntities = await _db.AttAttshifts
                .AsNoTracking()
                .Include(s => s.AttShiftdetails)
                    .ThenInclude(d => d.TimeInterval)
                .Where(s => shiftIds.Contains(s.Id))
                .ToListAsync();

            return shiftEntities.ToDictionary(
                shiftEntity => shiftEntity.Id,
                shiftEntity =>
                {
                    var baseSchedule = shiftEntity.AttShiftdetails
                        .Where(d => d.TimeInterval != null)
                        .Select(d => new ShiftBaseScheduleDto
                        {
                            DayIndex = d.DayIndex,
                            Alias = d.TimeInterval.Alias,
                            Id = d.TimeInterval.Id, // Asegúrate de que este campo exista en tu entidad
                            InTime = d.TimeInterval.InTime,
                            WorkTimeDurationMinutes = d.TimeInterval.WorkTimeDuration,
                            Duration = d.TimeInterval.Duration
                        })
                        .ToList();
                    return (shiftEntity.Id, shiftEntity.Alias, shiftEntity.ShiftCycle, baseSchedule);
                });
        }

        private async Task<List<ExceptionDto>> GetScheduleExceptionsAsync(List<int> assignmentIds, DateTime startDate, DateTime endDate)
        {
            // ... La lógica interna de este método es similar, solo cambia el .Where()
            return await _db.EmployeeScheduleExceptions
                .AsNoTracking()
                .Include(e => e.TimeInterval)
                .Where(e =>
                    assignmentIds.Contains(e.AssignmentId) && // <== CAMBIO AQUÍ
                    e.IsActive == 1 &&
                    e.ExceptionDate.HasValue &&
                    e.ExceptionDate.Value.Date >= startDate &&
                    e.ExceptionDate.Value.Date <= endDate &&
                    e.TimeInterval != null)
                .Select(e => new ExceptionDto {
                    ExceptionDate = e.ExceptionDate.Value.Date,
                    Alias = e.TimeInterval.Alias,
                    ExceptionId = e.ExceptionId, // Asumiendo que Id es el ID de la excepción
                    InTime = e.TimeInterval.InTime,
                    WorkTimeDurationMinutes = e.TimeInterval.WorkTimeDuration,
                    Duration = e.TimeInterval.Duration
                })
                .ToListAsync();
        }

        private List<ScheduleDayDto> BuildFinalSchedule(
     List<EmployeeShiftAssignment> assignments,
     Dictionary<int, (int Id, string Alias, int ShiftCycle, List<ShiftBaseScheduleDto> BaseSchedule)> shifts,
     List<ExceptionDto> exceptions,
     DateTime startDate,
     DateTime endDate)
        {
            var finalSchedule = new List<ScheduleDayDto>();
            var exceptionsDict = exceptions.ToDictionary(e => e.ExceptionDate.Date);

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // 1. Las excepciones siempre tienen la máxima prioridad.
                if (exceptionsDict.TryGetValue(date, out var exception))
                {
                    finalSchedule.Add(new ScheduleDayDto
                    {
                        Date = date,
                        DayName = GetDayName(date.DayOfWeek),
                        Alias = exception.Alias,
                        ScheduleId = exception.ExceptionId, // Asumiendo que ExceptionId es el ID del horario de excepción
                        InTime = exception.InTime.ToString("HH:mm"),
                        OutTime = exception.InTime.AddMinutes(exception.Duration).ToString("HH:mm"),
                        WorkTimeDurationMinutes = exception.WorkTimeDurationMinutes,
                        Duration = exception.Duration,
                        IsException = true,
                        
                    });
                    continue; // Pasamos al siguiente día
                }

                // 2. Si no hay excepción, buscamos la asignación y el turno para este día.
                var activeAssignment = assignments.FirstOrDefault(a => date >= a.StartDate && (a.EndDate == null || date <= a.EndDate));

                if (activeAssignment != null && shifts.TryGetValue(activeAssignment.ShiftId, out var activeShift))
                {
                    int dayIndex;

                    // --- INICIO DE LA LÓGICA DE ÍNDICE CORREGIDA ---
                    // Si el ciclo es 1 (o 7), se basa en el día de la semana.
                    // .NET: Domingo=0, Lunes=1, ..., Sábado=6
                    // Tu BD: 0=Domingo, 1=Lunes, ..., 6=Sábado. ¡Coinciden!
                    if (activeShift.ShiftCycle <= 7)
                    {
                        dayIndex = (int)date.DayOfWeek;
                    }
                    // Si el ciclo es mayor a 7 (ej. rotativo de 14 días), usamos la lógica de ciclo.
                    else
                    {
                        var daysSinceAssignmentStart = (date - activeAssignment.StartDate.Date).TotalDays;
                        dayIndex = (int)daysSinceAssignmentStart % activeShift.ShiftCycle;
                    }

                    var scheduleForDay = activeShift.BaseSchedule.FirstOrDefault(s => s.DayIndex == dayIndex);

                    if (scheduleForDay != null)
                    {
                        // Se encontró un horario de trabajo para este día
                        finalSchedule.Add(new ScheduleDayDto
                        {
                            Date = date,
                            DayName = GetDayName(date.DayOfWeek),
                            Alias = scheduleForDay.Alias,
                            ScheduleId = scheduleForDay.Id, // Asumiendo que Id es el ID del horario base
                            InTime = scheduleForDay.InTime.ToString("HH:mm"),
                            OutTime = scheduleForDay.InTime.AddMinutes(scheduleForDay.Duration).ToString("HH:mm"),
                            WorkTimeDurationMinutes = scheduleForDay.WorkTimeDurationMinutes,
                            Duration = scheduleForDay.Duration,
                            IsException = false,
                           
                        });
                    }
                    else
                    {
                        // El turno está activo, pero este día es libre (no tiene detalle de horario)
                        finalSchedule.Add(new ScheduleDayDto
                        {
                            Date = date,
                            DayName = GetDayName(date.DayOfWeek),
                            Alias = "Día Libre",
                            InTime = "--:--",
                            OutTime = "--:--",
                           
                        });
                    }
                }
                else
                {
                    // No se encontró ninguna asignación para este día
                    finalSchedule.Add(new ScheduleDayDto
                    {
                        Date = date,
                        DayName = GetDayName(date.DayOfWeek),
                        Alias = "Sin Asignación",
                        InTime = "--:--",
                        OutTime = "--:--",
                       
                    });
                }
            }
            return finalSchedule;
        }


        private (DateTime Start, DateTime End) GetCurrentWeekDateRange()
        {
            var today = DateTime.Today;
            // DayOfWeek en .NET tiene Domingo = 0, Lunes = 1, ...
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var monday = today.AddDays(-1 * diff);
            var sunday = monday.AddDays(6);
            return (monday, sunday);
        }

        private string GetDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => ""
            };
        }



    }

}
