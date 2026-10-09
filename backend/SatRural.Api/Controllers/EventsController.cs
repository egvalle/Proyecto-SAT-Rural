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

    public EventsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET /api/events
    // GET /api/events?communityId=1
    // GET /api/events?eventType=Helada
    // GET /api/events?level=RED
    // GET /api/events?from=2026-10-01&to=2026-10-07
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        [FromQuery] int? communityId,
        [FromQuery] string? eventType,
        [FromQuery] string? level,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _dbContext.Events
            .AsNoTracking()
            .AsQueryable();

        if (communityId.HasValue)
        {
            query = query.Where(
                eventItem => eventItem.CommunityId == communityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim();

            query = query.Where(
                eventItem => eventItem.EventType == normalizedEventType);
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalizedLevel = level.ToUpperInvariant();

            query = query.Where(
                eventItem =>
                    eventItem.Alert != null &&
                    eventItem.Alert.Level == normalizedLevel);
        }

        if (from.HasValue)
        {
            query = query.Where(
                eventItem => eventItem.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            var endDate = to.Value.Date.AddDays(1);

            query = query.Where(
                eventItem => eventItem.OccurredAt < endDate);
        }

        var events = await query
            .OrderByDescending(eventItem => eventItem.OccurredAt)
            .Select(eventItem => new EventDto
            {
                Id = eventItem.Id,
                AlertId = eventItem.AlertId,
                CommunityId = eventItem.CommunityId,
                CommunityName = eventItem.Community.Name,
                EventType = eventItem.EventType,
                Description = eventItem.Description,
                OccurredAt = eventItem.OccurredAt,
                SensorId = eventItem.Alert != null
                    ? eventItem.Alert.SensorId
                    : null,
                Level = eventItem.Alert != null
                    ? eventItem.Alert.Level
                    : null,
                IsActive = eventItem.Alert != null
                    ? eventItem.Alert.IsActive
                    : null
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET /api/events/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<EventDto>> GetEvent(long id)
    {
        var eventItem = await _dbContext.Events
            .AsNoTracking()
            .Where(eventItem => eventItem.Id == id)
            .Select(eventItem => new EventDto
            {
                Id = eventItem.Id,
                AlertId = eventItem.AlertId,
                CommunityId = eventItem.CommunityId,
                CommunityName = eventItem.Community.Name,
                EventType = eventItem.EventType,
                Description = eventItem.Description,
                OccurredAt = eventItem.OccurredAt,
                SensorId = eventItem.Alert != null
                    ? eventItem.Alert.SensorId
                    : null,
                Level = eventItem.Alert != null
                    ? eventItem.Alert.Level
                    : null,
                IsActive = eventItem.Alert != null
                    ? eventItem.Alert.IsActive
                    : null
            })
            .FirstOrDefaultAsync();

        if (eventItem is null)
        {
            return NotFound(new
            {
                message = "Evento no encontrado."
            });
        }

        return Ok(eventItem);
    }
}