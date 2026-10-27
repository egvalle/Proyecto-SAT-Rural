using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Application.Modules.Events;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AlertsDbContext _alertsDbContext;

    public EventsController(
        AppDbContext dbContext,
        AlertsDbContext alertsDbContext)
    {
        _dbContext = dbContext;
        _alertsDbContext = alertsDbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        [FromQuery] int? communityId,
        [FromQuery] string? eventType,
        [FromQuery] string? level,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Events.AsNoTracking().AsQueryable();

        if (communityId.HasValue)
        {
            query = query.Where(item => item.CommunityId == communityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim();
            query = query.Where(item => item.EventType == normalizedEventType);
        }

        if (from.HasValue)
        {
            query = query.Where(item => item.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            var endDate = to.Value.Date.AddDays(1);
            query = query.Where(item => item.OccurredAt < endDate);
        }

        var events = await query
            .OrderByDescending(item => item.OccurredAt)
            .Select(item => new EventDto
            {
                Id = item.Id,
                AlertId = item.AlertId,
                CommunityId = item.CommunityId,
                CommunityName = item.Community.Name,
                EventType = item.EventType,
                Description = item.Description,
                OccurredAt = item.OccurredAt
            })
            .ToListAsync(cancellationToken);

        await EnrichWithAlertsAsync(events, cancellationToken);

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedLevel = level.ToUpperInvariant();
            events = events
                .Where(item => item.Level == normalizedLevel)
                .ToList();
        }

        return Ok(events);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<EventDto>> GetEvent(
        long id,
        CancellationToken cancellationToken)
    {
        var eventItem = await _dbContext.Events
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new EventDto
            {
                Id = item.Id,
                AlertId = item.AlertId,
                CommunityId = item.CommunityId,
                CommunityName = item.Community.Name,
                EventType = item.EventType,
                Description = item.Description,
                OccurredAt = item.OccurredAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (eventItem is null)
        {
            return NotFound(new { message = "Evento no encontrado." });
        }

        await EnrichWithAlertsAsync([eventItem], cancellationToken);
        return Ok(eventItem);
    }

    private async Task EnrichWithAlertsAsync(
        List<EventDto> events,
        CancellationToken cancellationToken)
    {
        var alertIds = events
            .Where(item => item.AlertId.HasValue)
            .Select(item => item.AlertId!.Value)
            .Distinct()
            .ToArray();

        if (alertIds.Length == 0)
        {
            return;
        }

        var alertData = await _alertsDbContext.Alerts
            .AsNoTracking()
            .Where(alert => alertIds.Contains(alert.Id))
            .Select(alert => new
            {
                alert.Id,
                alert.SensorId,
                alert.Level,
                alert.IsActive
            })
            .ToDictionaryAsync(alert => alert.Id, cancellationToken);

        foreach (var eventItem in events)
        {
            if (eventItem.AlertId is not long alertId ||
                !alertData.TryGetValue(alertId, out var alert))
            {
                continue;
            }

            eventItem.SensorId = alert.SensorId;
            eventItem.Level = alert.Level;
            eventItem.IsActive = alert.IsActive;
        }
    }
}
