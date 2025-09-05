using Dtos.ManuaLog;
using Dtos.ResponseDto;
using Entities.ManualLog;
using FibAttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization; // Added for Authorize

namespace FibAttendanceApi.Controllers.AttManualLogController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Added Authorize at class level

    public class AttManuallogController : ControllerBase
    {
        private readonly ApplicationDbcontext _context;

        public AttManuallogController(ApplicationDbcontext context)
        {
            _context = context;
        }

        // GET: api/AttManuallog
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ResultadoConsulta<AttManuallogDto>>> GetAll(
         string? employeeId = null, int page = 1, int pageSize = 10)
        {
            var query = _context.AttManuallogs
                .GroupJoin(_context.EmployeeScheduleAssignments,
                    attLog => attLog.NroDoc,
                    empShift => empShift.NroDoc,
                    (attLog, empGroup) => new { attLog, empGroup })
                .SelectMany(x => x.empGroup.DefaultIfEmpty(),
                    (x, emp) => new { x.attLog, emp });

            // Aplicar filtro si se proporciona employeeId
            if (!string.IsNullOrWhiteSpace(employeeId))
                query = query.Where(x => x.attLog.NroDoc == employeeId );

            query = query.Where(
                x=>x.attLog.PunchTime>=x.emp.StartDate && x.attLog.PunchTime<= x.emp.EndDate);

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.attLog.PunchTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AttManuallogDto
                {
                    ManualLogId = x.attLog.ManuallogId,
                    AbstractexceptionPtrId = x.attLog.AbstractexceptionPtrId,
                    PunchTime = x.attLog.PunchTime,
                    PunchState = x.attLog.PunchState,
                    WorkCode = x.attLog.WorkCode,
                    ApplyReason = x.attLog.ApplyReason,
                    ApplyTime = x.attLog.ApplyTime,
                    AuditReason = x.attLog.AuditReason,
                    AuditTime = x.attLog.AuditTime,
                    ApprovalLevel = x.attLog.ApprovalLevel,
                    AuditUserId = x.attLog.AuditUserId,
                    Approver = x.attLog.Approver,
                    EmployeeId = x.attLog.EmployeeId ?? 0,
                    IsMask = x.attLog.IsMask,
                    Temperature = x.attLog.Temperature,
                    NroDoc = x.attLog.NroDoc,
                    FullName = x.emp != null ? x.emp.FullNameEmployee : null,
                    CreatedBy = x.attLog.CreatedBy,
                    CreatedAt = x.attLog.CreatedAt,
                    UpdatedBy = x.attLog.UpdatedBy,
                    UpdatedAt = x.attLog.UpdatedAt
                })
                .ToListAsync();

            var paginated = new PaginatedList<AttManuallogDto>(items, totalItems, page, pageSize);

            var resultado = new ResultadoConsulta<AttManuallogDto>
            {
                Exito = true,
                Mensaje = "Consulta exitosa.",
                Data = paginated
            };

            return Ok(resultado);
        }
        // POST: api/AttManuallog
        [HttpPost]
        public async Task<ActionResult<ResultadoConsulta<AttManuallogDto>>> Insert(
            [FromBody] List<AttManuallogCreateDto> dtos)
        {
            try
            {
                // Validación básica (opcional)
                if (dtos == null || !dtos.Any())
                {
                    return BadRequest("Debe enviar al menos un registro.");
                }

                var entities = dtos.Select(dto => new AttManuallog
                {
                    PunchTime = dto.PunchTime,
                    PunchState = dto.PunchState,
                    WorkCode = dto.WorkCode,
                    ApplyReason = dto.ApplyReason,
                    ApplyTime = dto.ApplyTime,
                    AuditReason = dto.AuditReason,
                    AuditTime = dto.AuditTime,
                    ApprovalLevel = dto.ApprovalLevel,
                    AuditUserId = dto.AuditUserId,
                    Approver = dto.Approver,
                    EmployeeId = dto.EmployeeId,
                    IsMask = dto.IsMask,
                    Temperature = dto.Temperature,
                    NroDoc = dto.NroDoc,
                    // fecha de creación y modificación, usuario que modifico y actualizo
                    CreatedBy = dto.CreatedBy
                }).ToList();

                _context.AttManuallogs.AddRange(entities);
                await _context.SaveChangesAsync();

                // Mapear la lista de entidades insertadas a DTOs para devolver
                var resultDtos = entities.Select(entity => new AttManuallogDto
                {
                    ManualLogId = entity.ManuallogId,
                    AbstractexceptionPtrId = entity.AbstractexceptionPtrId,
                    PunchTime = entity.PunchTime,
                    PunchState = entity.PunchState,
                    WorkCode = entity.WorkCode,
                    ApplyReason = entity.ApplyReason,
                    ApplyTime = entity.ApplyTime,
                    AuditReason = entity.AuditReason,
                    AuditTime = entity.AuditTime,
                    ApprovalLevel = entity.ApprovalLevel,
                    AuditUserId = entity.AuditUserId,
                    Approver = entity.Approver,
                    EmployeeId = entity.EmployeeId ?? 0,
                    IsMask = entity.IsMask,
                    Temperature = entity.Temperature,
                    NroDoc = entity.NroDoc,
                    
                }).ToList();

                // Para paginación, puedes ajustar los parámetros según lo que necesites (aquí se devuelve todo en una sola página)
                var paginated = new PaginatedList<AttManuallogDto>(
                    resultDtos, resultDtos.Count, 1, resultDtos.Count);

                var resultado = new ResultadoConsulta<AttManuallogDto>
                {
                    Exito = true,
                    Mensaje = "Registros creados exitosamente.",
                    Data = paginated
                };

                // Devuelve la lista de los IDs de los nuevos registros
                return CreatedAtAction(nameof(GetAll), null, resultado);
            }
            catch (Exception ex)
            {

                return BadRequest("Error en la Operacion "+ex);
            }
        }


        // PUT: api/AttManuallog/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ResultadoConsulta<AttManuallogDto>>> Update(int id, [FromBody] AttManuallogCreateDto dto)
        {
            var entity = await _context.AttManuallogs.FindAsync(id);
            if (entity == null)
            {
                return NotFound(new ResultadoConsulta<AttManuallogDto>
                {
                    Exito = false,
                    Mensaje = "Registro no encontrado.",
                    Data = null
                });
            }

            entity.PunchTime = dto.PunchTime;
            entity.PunchState = dto.PunchState;
            entity.WorkCode = dto.WorkCode;
            entity.ApplyReason = dto.ApplyReason;
            entity.ApplyTime = dto.ApplyTime;
            entity.AuditReason = dto.AuditReason;
            entity.AuditTime = dto.AuditTime;
            entity.ApprovalLevel = dto.ApprovalLevel;
            entity.AuditUserId = dto.AuditUserId;
            entity.Approver = dto.Approver;
            entity.EmployeeId = dto.EmployeeId;
            entity.IsMask = dto.IsMask;
            entity.Temperature = dto.Temperature;
            entity.NroDoc = dto.NroDoc;
            // Actualizar los campos de auditoría
            entity.UpdatedBy = dto.UpdatedBy;

            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var resultDto = new AttManuallogDto
            {
                ManualLogId = entity.ManuallogId,
                AbstractexceptionPtrId = entity.AbstractexceptionPtrId,
                PunchTime = entity.PunchTime,
                PunchState = entity.PunchState,
                WorkCode = entity.WorkCode,
                ApplyReason = entity.ApplyReason,
                ApplyTime = entity.ApplyTime,
                AuditReason = entity.AuditReason,
                AuditTime = entity.AuditTime,
                ApprovalLevel = entity.ApprovalLevel,
                AuditUserId = entity.AuditUserId,
                Approver = entity.Approver,
                EmployeeId = entity.EmployeeId ?? 0,
                IsMask = entity.IsMask,
                Temperature = entity.Temperature,
                NroDoc = entity.NroDoc

            };

            var paginated = new PaginatedList<AttManuallogDto>(
                new List<AttManuallogDto> { resultDto }, 1, 1, 1);

            var resultado = new ResultadoConsulta<AttManuallogDto>
            {
                Exito = true,
                Mensaje = "Registro actualizado exitosamente.",
                Data = paginated
            };

            return Ok(resultado);
        }

        // DELETE: api/AttManuallog/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultadoConsulta<AttManuallogDto>>> Delete(int id)
        {
            var entity = await _context.AttManuallogs.FindAsync(id);
            if (entity == null)
            {
                return NotFound(new ResultadoConsulta<AttManuallogDto>
                {
                    Exito = false,
                    Mensaje = "Registro no encontrado.",
                    Data = null
                });
            }

            _context.AttManuallogs.Remove(entity);
            await _context.SaveChangesAsync();

            var resultDto = new AttManuallogDto
            {
                ManualLogId = entity.ManuallogId,
                AbstractexceptionPtrId = entity.AbstractexceptionPtrId,
                PunchTime = entity.PunchTime,
                PunchState = entity.PunchState,
                WorkCode = entity.WorkCode,
                ApplyReason = entity.ApplyReason,
                ApplyTime = entity.ApplyTime,
                AuditReason = entity.AuditReason,
                AuditTime = entity.AuditTime,
                ApprovalLevel = entity.ApprovalLevel,
                AuditUserId = entity.AuditUserId,
                Approver = entity.Approver,
                EmployeeId = entity.EmployeeId ?? 0,
                IsMask = entity.IsMask,
                Temperature = entity.Temperature,
                NroDoc = entity.NroDoc
            };

            var paginated = new PaginatedList<AttManuallogDto>(
                new List<AttManuallogDto> { resultDto }, 1, 1, 1);

            var resultado = new ResultadoConsulta<AttManuallogDto>
            {
                Exito = true,
                Mensaje = "Registro eliminado exitosamente.",
                Data = paginated
            };

            return Ok(resultado);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultadoConsulta<AttManuallogDto>>> GetById(int id)
        {
            var result = await (from attLog in _context.AttManuallogs
                                join empShift in _context.EmployeeScheduleAssignments
                                    on attLog.NroDoc equals empShift.NroDoc into empGroup
                                from emp in empGroup.DefaultIfEmpty() // LEFT JOIN
                                where attLog.ManuallogId == id
                                select new AttManuallogDto
                                {
                                    ManualLogId = attLog.ManuallogId,
                                    AbstractexceptionPtrId = attLog.AbstractexceptionPtrId,
                                    PunchTime = attLog.PunchTime,
                                    PunchState = attLog.PunchState,
                                    WorkCode = attLog.WorkCode,
                                    ApplyReason = attLog.ApplyReason,
                                    ApplyTime = attLog.ApplyTime,
                                    AuditReason = attLog.AuditReason,
                                    AuditTime = attLog.AuditTime,
                                    ApprovalLevel = attLog.ApprovalLevel,
                                    AuditUserId = attLog.AuditUserId,
                                    Approver = attLog.Approver,
                                    EmployeeId = attLog.EmployeeId ?? 0,
                                    IsMask = attLog.IsMask,
                                    Temperature = attLog.Temperature,
                                    NroDoc = attLog.NroDoc,
                                    FullName = emp.FullNameEmployee, // 🎯 Aquí obtienes el nombre
                                    UpdatedBy = attLog.UpdatedBy,
                                    CreatedBy = attLog.CreatedBy,
                                    CreatedAt = attLog.CreatedAt,
                                    UpdatedAt = attLog.UpdatedAt
                                }).FirstOrDefaultAsync();

            if (result == null)
            {
                return NotFound(new ResultadoConsulta<AttManuallogDto>
                {
                    Exito = false,
                    Mensaje = "Registro no encontrado.",
                    Data = null
                });
            }

            var paginated = new PaginatedList<AttManuallogDto>(
                new List<AttManuallogDto> { result }, 1, 1, 1);

            var resultado = new ResultadoConsulta<AttManuallogDto>
            {
                Exito = true,
                Mensaje = "Consulta exitosa.",
                Data = paginated
            };

            return Ok(resultado);
        }
    }
}