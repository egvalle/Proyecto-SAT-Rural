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

    // GET: api/sensors
    [HttpGet]
    public async Task<ActionResult<PagedResult<SensorResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? code,
        [FromQuery] string? type,
        [FromQuery] int? communityId,
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(sensor =>
                sensor.Code.Contains(term) ||
                sensor.Name.Contains(term) ||
                sensor.Type.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var term = code.Trim();

            query = query.Where(sensor =>
                sensor.Code.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToUpper();

            query = query.Where(sensor =>
                sensor.Type == normalizedType);
        }

        if (communityId.HasValue)
        {
            query = query.Where(sensor =>
                sensor.CommunityId == communityId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(sensor =>
                sensor.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync();

        var sensors = await query
            .OrderBy(sensor => sensor.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sensor => new SensorResponse(
                sensor.Id,
                sensor.CommunityId,
                sensor.Community.Name,
                sensor.Code,
                sensor.Name,
                sensor.Type,
                sensor.Unit,
                sensor.Location,
                sensor.InstallationDate,
                sensor.Description,
                sensor.IsActive,
                sensor.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<SensorResponse>(
            sensors,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    // GET: api/sensors/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SensorResponse>> GetById(int id)
    {
        var sensor = await _dbContext.Sensors
            .AsNoTracking()
            .Include(sensor => sensor.Community)
            .FirstOrDefaultAsync(sensor => sensor.Id == id);

        if (sensor is null)
        {
            return NotFound(new
            {
                message = "Sensor not found."
            });
        }

        return Ok(ToResponse(sensor));
    }

    // POST: api/sensors
    [HttpPost]
    public async Task<ActionResult<SensorResponse>> Create(
        CreateSensorRequest request)
    {
        if (!ValidateRequiredFields(
                request.Code,
                request.Name,
                request.Type,
                request.Unit))
        {
            return BadRequest(new
            {
                message = "Code, name, type and unit are required."
            });
        }

        var community = await _dbContext.Communities
            .FirstOrDefaultAsync(community =>
                community.Id == request.CommunityId);

        if (community is null)
        {
            return BadRequest(new
            {
                message = "Community does not exist."
            });
        }

        var code = request.Code.Trim().ToUpper();

        if (await _dbContext.Sensors.AnyAsync(sensor =>
                sensor.Code == code))
        {
            return Conflict(new
            {
                message = "Sensor code is already registered."
            });
        }

        var sensor = new Sensor
        {
            CommunityId = request.CommunityId,
            Code = code,
            Name = request.Name.Trim(),
            Type = request.Type.Trim().ToUpper(),
            Unit = request.Unit.Trim(),
            Location = CleanOptionalText(request.Location),
            InstallationDate = request.InstallationDate,
            Description = CleanOptionalText(request.Description),
            IsActive = request.IsActive
        };

        _dbContext.Sensors.Add(sensor);
        await _dbContext.SaveChangesAsync();

        sensor.Community = community;

        return CreatedAtAction(
            nameof(GetById),
            new { id = sensor.Id },
            ToResponse(sensor));
    }

    // PUT: api/sensors/1
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SensorResponse>> Update(
        int id,
        UpdateSensorRequest request)
    {
        var sensor = await _dbContext.Sensors
            .Include(sensor => sensor.Community)
            .FirstOrDefaultAsync(sensor => sensor.Id == id);

        if (sensor is null)
        {
            return NotFound(new
            {
                message = "Sensor not found."
            });
        }

        if (!ValidateRequiredFields(
                request.Code,
                request.Name,
                request.Type,
                request.Unit))
        {
            return BadRequest(new
            {
                message = "Code, name, type and unit are required."
            });
        }

        var community = await _dbContext.Communities
            .FirstOrDefaultAsync(community =>
                community.Id == request.CommunityId);

        if (community is null)
        {
            return BadRequest(new
            {
                message = "Community does not exist."
            });
        }

        var code = request.Code.Trim().ToUpper();

        var duplicateCode = await _dbContext.Sensors
            .AnyAsync(other =>
                other.Id != id &&
                other.Code == code);

        if (duplicateCode)
        {
            return Conflict(new
            {
                message = "Sensor code is already registered."
            });
        }

        sensor.CommunityId = request.CommunityId;
        sensor.Community = community;
        sensor.Code = code;
        sensor.Name = request.Name.Trim();
        sensor.Type = request.Type.Trim().ToUpper();
        sensor.Unit = request.Unit.Trim();
        sensor.Location = CleanOptionalText(request.Location);
        sensor.InstallationDate = request.InstallationDate;
        sensor.Description = CleanOptionalText(request.Description);

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(sensor));
    }

    // PATCH: api/sensors/1/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<SensorResponse>> ChangeStatus(
        int id,
        ChangeSensorStatusRequest request)
    {
        var sensor = await _dbContext.Sensors
            .Include(sensor => sensor.Community)
            .FirstOrDefaultAsync(sensor => sensor.Id == id);

        if (sensor is null)
        {
            return NotFound(new
            {
                message = "Sensor not found."
            });
        }

        sensor.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(sensor));
    }

    private static bool ValidateRequiredFields(
        string code,
        string name,
        string type,
        string unit)
    {
        return !string.IsNullOrWhiteSpace(code) &&
               !string.IsNullOrWhiteSpace(name) &&
               !string.IsNullOrWhiteSpace(type) &&
               !string.IsNullOrWhiteSpace(unit);
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static SensorResponse ToResponse(Sensor sensor)
    {
        return new SensorResponse(
            sensor.Id,
            sensor.CommunityId,
            sensor.Community.Name,
            sensor.Code,
            sensor.Name,
            sensor.Type,
            sensor.Unit,
            sensor.Location,
            sensor.InstallationDate,
            sensor.Description,
            sensor.IsActive,
            sensor.CreatedAt);
    }
}

public sealed record CreateSensorRequest(
    int CommunityId,
    string Code,
    string Name,
    string Type,
    string Unit,
    string? Location,
    DateTime? InstallationDate,
    string? Description,
    bool IsActive = true);

public sealed record UpdateSensorRequest(
    int CommunityId,
    string Code,
    string Name,
    string Type,
    string Unit,
    string? Location,
    DateTime? InstallationDate,
    string? Description);

public sealed record ChangeSensorStatusRequest(
    bool IsActive);

public sealed record SensorResponse(
    int Id,
    int CommunityId,
    string CommunityName,
    string Code,
    string Name,
    string Type,
    string Unit,
    string? Location,
    DateTime? InstallationDate,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

    public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);