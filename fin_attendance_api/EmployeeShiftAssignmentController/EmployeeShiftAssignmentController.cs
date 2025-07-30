using Dtos.EmployeeShiftAssignmentDto;
using Dtos.ResponseDto;
using Entities.Manager;
using FibAttendanceApi.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FibAttendanceApi.Controllers
{
    [ApiController]
    [Route("api/employee-schedule-assignment")]
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
      [FromQuery] DateTime? endDate = null
  )
        {
            try
            {
                // Validar parámetros de entrada
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 15;

                // Query base
                var query = _db.EmployeeScheduleAssignments.AsQueryable();

                // Filtro por texto de búsqueda
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchLower = searchText.ToLower();
                    query = query.Where(x =>
                        x.FullNameEmployee.ToLower().Contains(searchLower) ||
                        x.ShiftDescription.ToLower().Contains(searchLower)
                    );
                }

                // Filtro por rango de fechas - CORREGIDO
                if (startDate.HasValue)
                {
                    // Convertir a fecha sin hora para comparación desde el inicio del día
                    var startDateOnly = startDate.Value.Date;
                    query = query.Where(x => x.StartDate >= startDateOnly);
                }

                if (endDate.HasValue)
                {
                    // Convertir a fecha sin hora y agregar 23:59:59 para incluir todo el día
                    var endDateOnly = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.EndDate <= endDateOnly);
                }

                // Ordenar por fecha de creación (más recientes primero)
                query = query.OrderByDescending(x => x.CreatedAt);

                // Contar total de registros
                var totalCount = await query.CountAsync();

                // Obtener registros paginados
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
                        LocationName = x.LocationName ?? "-" // Asegurarse de que LocationName no sea nulo
                    })
                    .ToListAsync();

                // Crear objeto paginado
                var paginated = new PaginatedList<EmployeeShiftAssignmentDTO>(items, totalCount, pageNumber, pageSize);

                // Crear resultado final
                var result = new ResultadoConsulta<EmployeeShiftAssignmentDTO>
                {
                    Exito = true, // Siempre es exitoso si no hay excepción
                    Mensaje = totalCount > 0 ? $"Se encontraron {totalCount} registros" : "No se encontraron registros",
                    Data = paginated
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log del error (asegúrate de tener un logger configurado)
                // _logger.LogError(ex, "Error en búsqueda de asignaciones de horarios");

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
            [FromBody] List<EmployeeShiftAssignment> models)
        {
            var resultado = new ResultadoConsulta<EmployeeShiftAssignmentDTO>();
            try
            {
                if (models == null || !models.Any())
                {
                    resultado.Exito = false;
                    resultado.Mensaje = "El arreglo no debe estar vacío.";
                    resultado.Data = null;
                    return BadRequest(resultado);
                }

                // Agrega todos los modelos de una sola vez
                _db.EmployeeScheduleAssignments.AddRange(models);
                await _db.SaveChangesAsync();

                // Mapea cada modelo a su DTO
                var dtos = models.Select(model => new EmployeeShiftAssignmentDTO
                {
                    AssignmentId = model.AssignmentId,
                    EmployeeId = model.EmployeeId,
                    FullNameEmployee = model.FullNameEmployee,
                    ScheduleName = model.ShiftDescription,
                    ScheduleId = model.ShiftId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Remarks = model.Remarks,
                    CreatedAt = model.CreatedAt,
                    NroDoc = model.NroDoc ?? "-",
                    AreaId = model.AreaId,
                    AreaName = model.AreaDescription ?? "-",
                    CreatedWeek = ISOWeek.GetWeekOfYear(model.CreatedAt),
                    LocationId = model.LocationId,
                    LocationName = model.LocationName ?? "-"
                }).ToList();

                resultado.Exito = true;
                resultado.Mensaje = "Asignaciones creadas correctamente.";
                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(dtos, dtos.Count, 1, dtos.Count);
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = ex.Message;
                resultado.Data = null;
            }
            return Ok(resultado);
        }




        // PUT: api/employee-schedule-assignment/update/{id}
        [HttpPut("update/")]
        public async Task<ActionResult<ResultadoConsulta<EmployeeShiftAssignmentDTO>>> UpdateAssignment(
          [FromBody] IEnumerable<EmployeeShiftAssignment> models)
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
                    // Assuming each model in the array has an AssignmentId to identify the entity
                    if (model.AssignmentId == 0) // Or whatever your default/invalid ID is
                    {
                        failedUpdates.Add($"Assignment with missing ID skipped.");
                        continue;
                    }

                    var entity = await _db.EmployeeScheduleAssignments.FindAsync(model.AssignmentId);
                    if (entity == null)
                    {
                        failedUpdates.Add($"Assignment with ID {model.AssignmentId} not found.");
                        continue; // Skip to the next item
                    }

                    // Update properties from the model
                    entity.ShiftId = model.ShiftId;
                    entity.StartDate = model.StartDate;
                    entity.EndDate = model.EndDate;
                    entity.Remarks = model.Remarks;

                    // Mark entity as modified
                    _db.EmployeeScheduleAssignments.Update(entity);
                    // SaveChangesAsync will be called once after the loop for efficiency

                    // Create DTO for the updated entity
                    var dto = new EmployeeShiftAssignmentDTO
                    {
                        AssignmentId = entity.AssignmentId,
                        EmployeeId = entity.EmployeeId,
                        ScheduleName = entity.ShiftDescription, // Ensure these properties are correctly mapped in your DTO/entity
                        ScheduleId = entity.ShiftId,
                        FullNameEmployee = entity.FullNameEmployee,
                        StartDate = entity.StartDate,
                        EndDate = entity.EndDate,
                        Remarks = entity.Remarks,
                        CreatedAt = entity.CreatedAt,
                        CreatedWeek = ISOWeek.GetWeekOfYear(entity.CreatedAt),
                        NroDoc = entity.NroDoc ?? "-",
                        AreaId = entity.AreaId,
                        AreaName = entity.AreaDescription ?? "-",
                        LocationId = entity.LocationId,
                        LocationName = entity.LocationName ?? "-"
                    };
                    updatedAssignments.Add(dto);
                }
                catch (Exception ex)
                {
                    failedUpdates.Add($"Failed to update assignment with ID {model.AssignmentId}: {ex.Message}");
                    // You might want to log the exception here
                }
            }

            try
            {
                await _db.SaveChangesAsync(); // Save all changes in one go
                resultado.Exito = true;
                resultado.Mensaje = updatedAssignments.Any()
                    ? $"Successfully updated {updatedAssignments.Count} assignments."
                    : "No assignments were updated.";

                if (failedUpdates.Any())
                {
                    resultado.Mensaje += $" Some updates failed: {string.Join("; ", failedUpdates)}";
                    // You might want to adjust the Exito flag here based on your business rules
                    // If even one fails, set Exito to false, or keep true if some succeed.
                    // For now, I'm keeping Exito true if at least one update succeeds.
                }

                resultado.Data = new PaginatedList<EmployeeShiftAssignmentDTO>(
                    updatedAssignments, updatedAssignments.Count, 1, updatedAssignments.Count);
            }
            catch (DbUpdateException dbEx)
            {
                resultado.Exito = false;
                resultado.Mensaje = $"An error occurred while saving changes: {dbEx.InnerException?.Message ?? dbEx.Message}";
                resultado.Data = null;
                // Log the full DbUpdateException for debugging
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = $"An unexpected error occurred: {ex.Message}";
                resultado.Data = null;
                // Log the unexpected exception
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



    }

}
