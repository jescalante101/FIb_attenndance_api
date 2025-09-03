using Dtos.ScheduleExceptionsDto;
using Entities.Shifts;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization; // Added for Authorize

namespace FibAttendanceApi.Controllers.EmployeeScheduleExceptionController
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Added Authorize at class level
    public class EmployeeScheduleExceptionController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;

        public EmployeeScheduleExceptionController(ApplicationDbcontext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las excepciones de horarios
        /// </summary>
        /// <returns>Lista de excepciones</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EmployeeScheduleException>>> GetAllExceptions()
        {
            try
            {
                var exceptions = await _context.EmployeeScheduleExceptions
                    .Where(e => e.IsActive == 1)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Excepciones obtenidas correctamente",
                    data = exceptions,
                    count = exceptions.Count() // Cambiar "exceptions.Count" a "exceptions.Count()"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene una excepción por ID
        /// </summary>
        /// <param name="id">ID de la excepción</param>
        /// <returns>Excepción encontrada</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeScheduleException>> GetExceptionById(int id)
        {
            try
            {
                var exception = await _context.EmployeeScheduleExceptions
                    .FirstOrDefaultAsync(e => e.ExceptionId == id && e.IsActive == 1);

                if (exception == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró la excepción con ID {id}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Excepción encontrada correctamente",
                    data = exception
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene excepciones por empleado
        /// </summary>
        /// <param name="employeeId">ID del empleado</param>
        /// <returns>Lista de excepciones del empleado</returns>
        [HttpGet("employee/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EmployeeScheduleException>>> GetExceptionsByEmployee(
            [Required] string employeeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El ID del empleado es requerido"
                    });
                }

                var exceptions = await _context.EmployeeScheduleExceptions
                    .Where(e => e.EmployeeId == employeeId && e.IsActive == 1)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Excepciones del empleado {employeeId} obtenidas correctamente",
                    data = exceptions,
                    count = exceptions.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene excepciones por fecha específica
        /// </summary>
        /// <param name="date">Fecha a consultar</param>
        /// <returns>Lista de excepciones para la fecha</returns>
        [HttpGet("date/{date}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EmployeeScheduleException>>> GetExceptionsByDate(
            [Required] DateTime date)
        {
            try
            {
                var exceptions = await _context.EmployeeScheduleExceptions
                    .Where(e => e.ExceptionDate.HasValue &&
                               e.ExceptionDate.Value.Date == date.Date &&
                               e.IsActive == 1)
                    .OrderBy(e => e.EmployeeId)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Excepciones para la fecha {date:yyyy-MM-dd} obtenidas correctamente",
                    data = exceptions,
                    count = exceptions.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Crea una nueva excepción de horario
        /// </summary>
        /// <param name="exception">Datos de la excepción</param>
        /// <returns>Excepción creada</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeScheduleException>> CreateException(
            [FromBody] EmployeeScheduleExceptionCreateDto exceptionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Datos de entrada inválidos",
                        errors = ModelState
                    });
                }

                // Validaciones de negocio
                var validationResult = await ValidateException(exceptionDto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = validationResult.ErrorMessage
                    });
                }

                var exception = new EmployeeScheduleException
                {
                    EmployeeId = exceptionDto.EmployeeId,
                    AssignmentId = exceptionDto.AssignmentId,
                    ExceptionDate = exceptionDto.ExceptionDate,
                    DayIndex = exceptionDto.DayIndex,
                    TimeIntervalId = exceptionDto.TimeIntervalId,
                    ExceptionType = exceptionDto.ExceptionType,
                    StartDate = exceptionDto.StartDate,
                    EndDate = exceptionDto.EndDate,
                    Remarks = exceptionDto.Remarks,
                    IsActive = 1,
                    CreatedAt = DateTime.Now,
                    CreatedBy = exceptionDto.CreatedBy,
                    UpdatedAt = DateTime.Now
                };

                _context.EmployeeScheduleExceptions.Add(exception);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetExceptionById),
                    new { id = exception.ExceptionId },
                    new
                    {
                        success = true,
                        message = "Excepción de horario creada correctamente",
                        data = exception
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Actualiza una excepción existente
        /// </summary>
        /// <param name="id">ID de la excepción</param>
        /// <param name="exceptionDto">Datos actualizados</param>
        /// <returns>Excepción actualizada</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateException(int id,
            [FromBody] EmployeeScheduleExceptionUpdateDto exceptionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Datos de entrada inválidos",
                        errors = ModelState
                    });
                }

                var existingException = await _context.EmployeeScheduleExceptions
                    .FirstOrDefaultAsync(e => e.ExceptionId == id && e.IsActive == 1);

                if (existingException == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró la excepción con ID {id}"
                    });
                }

                // Actualizar campos
                existingException.AssignmentId = exceptionDto.AssignmentId;
                existingException.ExceptionDate = exceptionDto.ExceptionDate;
                existingException.DayIndex = exceptionDto.DayIndex;
                existingException.TimeIntervalId = exceptionDto.TimeIntervalId;
                existingException.ExceptionType = exceptionDto.ExceptionType;
                existingException.StartDate = exceptionDto.StartDate;
                existingException.EndDate = exceptionDto.EndDate;
                existingException.Remarks = exceptionDto.Remarks;
                existingException.UpdatedAt = DateTime.Now;
                existingException.UpdatedBy = exceptionDto.UpdatedBy;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Excepción actualizada correctamente",
                    data = existingException
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Elimina (desactiva) una excepción
        /// </summary>
        /// <param name="id">ID de la excepción</param>
        /// <returns>Confirmación de eliminación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteException(int id)
        {
            try
            {
                var exception = await _context.EmployeeScheduleExceptions
                    .FirstOrDefaultAsync(e => e.ExceptionId == id && e.IsActive == 1);

                if (exception == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró la excepción con ID {id}"
                    });
                }

                // Eliminación lógica
                exception.IsActive = 0;
                exception.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Excepción eliminada correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Activa/Desactiva una excepción
        /// </summary>
        /// <param name="id">ID de la excepción</param>
        /// <param name="isActive">Estado activo (true/false)</param>
        /// <returns>Confirmación del cambio de estado</returns>
        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ToggleExceptionStatus(int id, [FromQuery] bool isActive)
        {
            try
            {
                var exception = await _context.EmployeeScheduleExceptions
                    .FirstOrDefaultAsync(e => e.ExceptionId == id);

                if (exception == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"No se encontró la excepción con ID {id}"
                    });
                }

                exception.IsActive = (byte)(isActive ? 1 : 0);
                exception.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Excepción {(isActive ? "activada" : "desactivada")} correctamente",
                    data = new { id = exception.ExceptionId, isActive = exception.IsActive }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        #region Private Methods

        private async Task<(bool IsValid, string ErrorMessage)> ValidateException(
            EmployeeScheduleExceptionCreateDto exceptionDto)
        {
            // Validar que el tipo de excepción coincida con los datos
            if (exceptionDto.ExceptionType == 1 && !exceptionDto.ExceptionDate.HasValue)
            {
                return (false, "Para excepciones de fecha específica, debe proporcionar la fecha");
            }

            if (exceptionDto.ExceptionType == 2 && !exceptionDto.DayIndex.HasValue)
            {
                return (false, "Para excepciones recurrentes, debe proporcionar el día de la semana");
            }

            // Validar que existe el time_interval_id
            var timeIntervalExists = await _context.Set<AttTimeinterval>()
                .AnyAsync(t => t.Id == exceptionDto.TimeIntervalId);

            if (!timeIntervalExists)
            {
                return (false, $"No existe el horario con ID {exceptionDto.TimeIntervalId}");
            }

            // Validar fechas
            if (exceptionDto.StartDate.HasValue && exceptionDto.EndDate.HasValue &&
                exceptionDto.StartDate.Value > exceptionDto.EndDate.Value)
            {
                return (false, "La fecha de inicio no puede ser mayor a la fecha de fin");
            }

            return (true, string.Empty);
        }

        #endregion
    }

   
}