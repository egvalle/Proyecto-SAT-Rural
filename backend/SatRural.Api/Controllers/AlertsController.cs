using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SatRural.Application.Modules.Alerts;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AlertsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET /api/alerts?level=RED&isActive=true&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(
        [FromQuery] string? level,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Alerts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedLevel = level.ToUpperInvariant();
            query = query.Where(alert => alert.Level == normalizedLevel);
        }

        if (isActive.HasValue)
        {
            query = query.Where(alert => alert.IsActive == isActive.Value);
        }

        var alerts = await query
            .OrderByDescending(alert => alert.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(alert => new AlertDto
            {
                Id = alert.Id,
                CommunityId = alert.CommunityId,
                SensorId = alert.SensorId,
                Type = alert.Type,
                Level = alert.Level,
                Message = alert.Message,
                CreatedAt = alert.CreatedAt,
                ResolvedAt = alert.ResolvedAt,
                IsActive = alert.IsActive
            })
            .ToListAsync();

        return Ok(alerts);
    }

    // PATCH /api/alerts/5/resolve
    [HttpPatch("{id:long}/resolve")]
    public async Task<IActionResult> ResolveAlert(long id)
    {
        var alert = await _dbContext.Alerts
            .FirstOrDefaultAsync(a => a.Id == id);

        if (alert is null)
        {
            return NotFound(new { message = "Alerta no encontrada." });
        }

        if (!alert.IsActive)
        {
            return Ok(new { message = "La alerta ya estaba resuelta." });
        }

        alert.IsActive = false;
        alert.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Alerta marcada como resuelta." });
    }
}