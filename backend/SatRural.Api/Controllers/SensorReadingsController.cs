using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/sensor-readings")]
public class SensorReadingsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public SensorReadingsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET: api/sensor-readings
    [HttpGet]
    public async Task<ActionResult<PagedResult<SensorReadingResponse>>> Get(
        [FromQuery] int? sensorId,
        [FromQuery] int? communityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be greater than 0 and pageSize must be between 1 and 100."
            });
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new
            {
                message = "The start date cannot be greater than the end date."
            });
        }

        var query = _dbContext.SensorReadings
            .AsNoTracking()
            .Include(reading => reading.Sensor)
                .ThenInclude(sensor => sensor.Community)
            .AsQueryable();

        if (sensorId.HasValue)
        {
            query = query.Where(reading =>
                reading.SensorId == sensorId.Value);
        }

        if (communityId.HasValue)
        {
            query = query.Where(reading =>
                reading.Sensor.CommunityId == communityId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(reading =>
                reading.RecordedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(reading =>
                reading.RecordedAt <= to.Value);
        }

        var totalItems = await query.CountAsync();

        var readings = await query
            .OrderByDescending(reading => reading.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(reading => new SensorReadingResponse(
                reading.Id,
                reading.SensorId,
                reading.Sensor.Code,
                reading.Sensor.Name,
                reading.Sensor.Type,
                reading.Sensor.CommunityId,
                reading.Sensor.Community.Name,
                reading.Value,
                reading.Sensor.Unit,
                reading.Sensor.IsActive,
                reading.RecordedAt))
            .ToListAsync();

        return Ok(new PagedResult<SensorReadingResponse>(
            readings,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    // GET: api/sensor-readings/sensor/1
    [HttpGet("sensor/{sensorId:int}")]
    public async Task<ActionResult<PagedResult<SensorReadingResponse>>> GetBySensor(
        int sensorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be greater than 0 and pageSize must be between 1 and 100."
            });
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new
            {
                message = "The start date cannot be greater than the end date."
            });
        }

        var sensorExists = await _dbContext.Sensors
            .AsNoTracking()
            .AnyAsync(sensor => sensor.Id == sensorId);

        if (!sensorExists)
        {
            return NotFound(new
            {
                message = "Sensor not found."
            });
        }

        var query = _dbContext.SensorReadings
            .AsNoTracking()
            .Where(reading => reading.SensorId == sensorId);

        if (from.HasValue)
        {
            query = query.Where(reading =>
                reading.RecordedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(reading =>
                reading.RecordedAt <= to.Value);
        }

        var totalItems = await query.CountAsync();

        var readings = await query
            .OrderByDescending(reading => reading.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(reading => new SensorReadingResponse(
                reading.Id,
                reading.SensorId,
                reading.Sensor.Code,
                reading.Sensor.Name,
                reading.Sensor.Type,
                reading.Sensor.CommunityId,
                reading.Sensor.Community.Name,
                reading.Value,
                reading.Sensor.Unit,
                reading.Sensor.IsActive,
                reading.RecordedAt))
            .ToListAsync();

        return Ok(new PagedResult<SensorReadingResponse>(
            readings,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    // GET: api/sensor-readings/latest
    [HttpGet("latest")]
    public async Task<ActionResult<IReadOnlyCollection<SensorReadingResponse>>> GetLatest(
        [FromQuery] int? communityId)
    {
        var sensorsQuery = _dbContext.Sensors
    .AsNoTracking()
    .Where(sensor => sensor.IsActive)
    .AsQueryable();
    

        if (communityId.HasValue)
        {
            sensorsQuery = sensorsQuery.Where(sensor =>
                sensor.CommunityId == communityId.Value);
        }

        var sensorIds = await sensorsQuery
            .Select(sensor => sensor.Id)
            .ToListAsync();

        var latestReadings = new List<SensorReadingResponse>();

        foreach (var sensorId in sensorIds)
        {
            var reading = await _dbContext.SensorReadings
                .AsNoTracking()
                .Where(reading => reading.SensorId == sensorId)
                .OrderByDescending(reading => reading.RecordedAt)
                .Select(reading => new SensorReadingResponse(
                    reading.Id,
                    reading.SensorId,
                    reading.Sensor.Code,
                    reading.Sensor.Name,
                    reading.Sensor.Type,
                    reading.Sensor.CommunityId,
                    reading.Sensor.Community.Name,
                    reading.Value,
                    reading.Sensor.Unit,
                    reading.Sensor.IsActive,
                    reading.RecordedAt))
                .FirstOrDefaultAsync();

            if (reading is not null)
            {
                latestReadings.Add(reading);
            }
        }

        return Ok(latestReadings
            .OrderBy(reading => reading.SensorId)
            .ToList());
    }
}

public sealed record SensorReadingResponse(
    long Id,
    int SensorId,
    string SensorCode,
    string SensorName,
    string SensorType,
    int CommunityId,
    string CommunityName,
    decimal Value,
    string Unit,
    bool SensorIsActive,
    DateTime RecordedAt);