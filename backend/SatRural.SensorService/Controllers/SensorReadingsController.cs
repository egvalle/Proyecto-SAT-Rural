using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Entities;
using SatRural.SensorService.Persistence;

namespace SatRural.SensorService.Controllers;

[ApiController]
[Authorize]
[Route("api/sensor-readings")]
public sealed class SensorReadingsController : ControllerBase
{
    private readonly SensorDbContext _dbContext;
    private readonly IHttpClientFactory _clients;

    public SensorReadingsController(
        SensorDbContext dbContext, IHttpClientFactory clients)
    {
        _dbContext = dbContext;
        _clients = clients;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DetailedReadingResponse>>> Get(
        [FromQuery] int? sensorId,
        [FromQuery] int? communityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 ||
            (from.HasValue && to.HasValue && from > to))
        {
            return BadRequest(new { message = "Invalid pagination or date range." });
        }

        var query = _dbContext.SensorReadings
            .AsNoTracking()
            .Include(reading => reading.Sensor)
            .AsQueryable();
        if (sensorId.HasValue)
            query = query.Where(reading => reading.SensorId == sensorId.Value);
        if (communityId.HasValue)
            query = query.Where(reading =>
                reading.Sensor.CommunityId == communityId.Value);
        if (from.HasValue)
            query = query.Where(reading => reading.RecordedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(reading => reading.RecordedAt <= to.Value);

        var totalItems = await query.CountAsync(cancellationToken);
        var readings = await query
            .OrderByDescending(reading => reading.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = await ToResponsesAsync(readings, cancellationToken);
        return Ok(new PagedResult<DetailedReadingResponse>(
            items, page, pageSize, totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    [HttpGet("sensor/{sensorId:int}")]
    public Task<ActionResult<PagedResult<DetailedReadingResponse>>> GetBySensor(
        int sensorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Get(sensorId, null, from, to, page, pageSize, cancellationToken);

    [HttpGet("latest")]
    public async Task<ActionResult<IReadOnlyCollection<DetailedReadingResponse>>> Latest(
        [FromQuery] int? communityId,
        CancellationToken cancellationToken)
    {
        var sensors = await _dbContext.Sensors.AsNoTracking()
            .Where(sensor => sensor.IsActive &&
                (!communityId.HasValue || sensor.CommunityId == communityId.Value))
            .ToListAsync(cancellationToken);
        var readings = new List<SensorReading>();
        foreach (var sensor in sensors)
        {
            var last = await _dbContext.SensorReadings.AsNoTracking()
                .Include(reading => reading.Sensor)
                .Where(reading => reading.SensorId == sensor.Id)
                .OrderByDescending(reading => reading.RecordedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (last is not null) readings.Add(last);
        }
        return Ok(await ToResponsesAsync(readings, cancellationToken));
    }

    private async Task<List<DetailedReadingResponse>> ToResponsesAsync(
        IEnumerable<SensorReading> readings, CancellationToken cancellationToken)
    {
        var list = readings.ToList();
        var names = new Dictionary<int, string>();
        var client = _clients.CreateClient("CommunityService");
        foreach (var id in list.Select(item => item.Sensor.CommunityId).Distinct())
        {
            var community = await client.GetFromJsonAsync<CommunityLookup>(
                $"api/communities/{id}", cancellationToken);
            names[id] = community?.Name ?? $"Comunidad {id}";
        }
        return list.Select(reading => new DetailedReadingResponse(
            reading.Id, reading.SensorId, reading.Sensor.Code,
            reading.Sensor.Name, reading.Sensor.Type,
            reading.Sensor.CommunityId,
            names[reading.Sensor.CommunityId], reading.Value,
            reading.Sensor.Unit, reading.Sensor.IsActive,
            reading.RecordedAt, reading.Source)).ToList();
    }
}

public sealed record DetailedReadingResponse(
    long Id, int SensorId, string SensorCode, string SensorName,
    string SensorType, int CommunityId, string CommunityName,
    decimal Value, string Unit, bool SensorIsActive,
    DateTime RecordedAt, string Source);
