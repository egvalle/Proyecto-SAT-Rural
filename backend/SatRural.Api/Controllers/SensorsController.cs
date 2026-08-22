using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/sensors")]
public class SensorsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public SensorsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SensorResponse>>> Get(
        [FromQuery] string? sensor,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page must be greater than 0 and pageSize must be between 1 and 100."
            });
        }

        var query = _dbContext.Sensors
            .AsNoTracking()
            .Include(sensor => sensor.Community)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(sensor))
        {
            var searchTerm = sensor.Trim();
            query = query.Where(sensor =>
                sensor.Code.Contains(searchTerm) ||
                sensor.Name.Contains(searchTerm) ||
                sensor.Type.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(sensor => sensor.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();
        var sensors = await query
            .OrderBy(sensor => sensor.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sensor => ToResponse(sensor))
            .ToListAsync();

        return Ok(new PagedResult<SensorResponse>(
            sensors,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    [HttpPost]
    public async Task<ActionResult<SensorResponse>> Create(CreateSensorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Type) ||
            string.IsNullOrWhiteSpace(request.Unit))
        {
            return BadRequest(new
            {
                message = "Code, name, type and unit are required."
            });
        }

        if (!await _dbContext.Communities.AnyAsync(
                community => community.Id == request.CommunityId))
        {
            return BadRequest(new { message = "Community does not exist." });
        }

        var code = request.Code.Trim();
        if (await _dbContext.Sensors.AnyAsync(sensor => sensor.Code == code))
        {
            return Conflict(new { message = "Sensor code is already registered." });
        }

        var sensor = new Sensor
        {
            CommunityId = request.CommunityId,
            Code = code,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Unit = request.Unit.Trim(),
            IsActive = request.IsActive
        };

        _dbContext.Sensors.Add(sensor);
        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(sensor)
            .Reference(item => item.Community)
            .LoadAsync();

        return CreatedAtAction(
            nameof(Get),
            new { sensor = sensor.Code, page = 1, pageSize = 1 },
            ToResponse(sensor));
    }

    private static SensorResponse ToResponse(Sensor sensor) =>
        new(
            sensor.Id,
            sensor.CommunityId,
            sensor.Community.Name,
            sensor.Code,
            sensor.Name,
            sensor.Type,
            sensor.Unit,
            sensor.IsActive,
            sensor.CreatedAt);
}

public sealed record CreateSensorRequest(
    int CommunityId,
    string Code,
    string Name,
    string Type,
    string Unit,
    bool IsActive = true);

public sealed record SensorResponse(
    int Id,
    int CommunityId,
    string CommunityName,
    string Code,
    string Name,
    string Type,
    string Unit,
    bool IsActive,
    DateTime CreatedAt);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);