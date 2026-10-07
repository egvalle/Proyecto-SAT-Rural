using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatRural.Application.Modules.Audit;
using SatRural.Application.Modules.Audit.Interfaces;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "ADMIN")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var auditLogs = await _auditLogService.GetAllAsync(
            cancellationToken);

        return Ok(auditLogs);
    }
}