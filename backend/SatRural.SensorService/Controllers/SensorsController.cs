using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Entities;
using SatRural.SensorService.Persistence;
using SatRural.SensorService.Services;

namespace SatRural.SensorService.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly SensorDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SensorsController> _logger;
    private readonly AuditLogClient _audit;

    public SensorsController(
        SensorDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SensorsController> logger,
        AuditLogClient audit)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _audit = audit;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResult<SensorResponse>>> GetSensors(
        [FromQuery] string? search,
        [FromQuery] string? code,
        [FromQuery] string? type,
        [FromQuery] int? communityId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(new { message = "Invalid page or pageSize." });
        }

        var query = _dbContext.Sensors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(sensor =>
                sensor.Code.Contains(search) || sensor.Name.Contains(search) ||
                sensor.Type.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(code))
            query = query.Where(sensor => sensor.Code.Contains(code));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(sensor => sensor.Type == type.ToUpper());
        if (communityId.HasValue)
            query = query.Where(sensor => sensor.CommunityId == communityId.Value);
        if (isActive.HasValue)
            query = query.Where(sensor => sensor.IsActive == isActive.Value);

        var totalItems = await query.CountAsync(cancellationToken);
        var sensors = await query
            .OrderBy(sensor => sensor.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var responses = await Task.WhenAll(sensors.Select(sensor =>
            ToResponseAsync(sensor, cancellationToken)));

        return Ok(new PagedResult<SensorResponse>(
            responses, page, pageSize, totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<SensorResponse>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var sensor = await _dbContext.Sensors.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sensor is null)
            return NotFound(new { message = "Sensor not found." });
        return Ok(await ToResponseAsync(sensor, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<ActionResult<SensorResponse>> Create(
        CreateSensorRequest request, CancellationToken cancellationToken)
    {
        if (!Valid(request.Code, request.Name, request.Type, request.Unit))
            return BadRequest(new { message = "Code, name, type and unit are required." });
        var community = await GetCommunityAsync(request.CommunityId, cancellationToken);
        if (community is null)
            return BadRequest(new { message = "Community does not exist." });
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _dbContext.Sensors.AnyAsync(
            item => item.Code == normalizedCode, cancellationToken))
            return Conflict(new { message = "Sensor code is already registered." });

        var sensor = new Sensor
        {
            CommunityId = request.CommunityId,
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Type = request.Type.Trim().ToUpperInvariant(),
            Unit = request.Unit.Trim(),
            Location = Clean(request.Location),
            InstallationDate = request.InstallationDate,
            Description = Clean(request.Description),
            IsActive = request.IsActive
        };
        _dbContext.Sensors.Add(sensor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(User, "CREATE", "Sensor", sensor.Id,
            $"Sensor creado: {sensor.Code}", cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sensor.Id },
            ToResponse(sensor, community.Name));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<ActionResult<SensorResponse>> Update(
        int id, UpdateSensorRequest request, CancellationToken cancellationToken)
    {
        var sensor = await _dbContext.Sensors
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sensor is null)
            return NotFound(new { message = "Sensor not found." });
        if (!Valid(request.Code, request.Name, request.Type, request.Unit))
            return BadRequest(new { message = "Code, name, type and unit are required." });
        var community = await GetCommunityAsync(request.CommunityId, cancellationToken);
        if (community is null)
            return BadRequest(new { message = "Community does not exist." });
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _dbContext.Sensors.AnyAsync(
            item => item.Id != id && item.Code == normalizedCode, cancellationToken))
            return Conflict(new { message = "Sensor code is already registered." });

        sensor.CommunityId = request.CommunityId;
        sensor.Code = normalizedCode;
        sensor.Name = request.Name.Trim();
        sensor.Type = request.Type.Trim().ToUpperInvariant();
        sensor.Unit = request.Unit.Trim();
        sensor.Location = Clean(request.Location);
        sensor.InstallationDate = request.InstallationDate;
        sensor.Description = Clean(request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(User, "UPDATE", "Sensor", sensor.Id,
            $"Sensor actualizado: {sensor.Code}", cancellationToken);
        return Ok(ToResponse(sensor, community.Name));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<ActionResult<SensorResponse>> ChangeStatus(
        int id, ChangeSensorStatusRequest request, CancellationToken cancellationToken)
    {
        var sensor = await _dbContext.Sensors
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sensor is null)
            return NotFound(new { message = "Sensor not found." });
        sensor.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(User, "STATUS", "Sensor", sensor.Id,
            $"Estado activo: {sensor.IsActive}", cancellationToken);
        return Ok(await ToResponseAsync(sensor, cancellationToken));
    }

    private async Task<CommunityLookup?> GetCommunityAsync(
        int id, CancellationToken cancellationToken)
    {
        if (id < 1) return null;
        var client = _httpClientFactory.CreateClient("CommunityService");
        using var response = await client.GetAsync(
            $"api/communities/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommunityLookup>(cancellationToken);
    }

    private async Task<SensorResponse> ToResponseAsync(
        Sensor sensor, CancellationToken cancellationToken)
    {
        var community = await GetCommunityAsync(sensor.CommunityId, cancellationToken);
        return ToResponse(sensor, community?.Name ?? $"Comunidad {sensor.CommunityId}");
    }

    private static SensorResponse ToResponse(Sensor sensor, string communityName) =>
        new(sensor.Id, sensor.CommunityId, communityName, sensor.Code,
            sensor.Name, sensor.Type, sensor.Unit, sensor.Location,
            sensor.InstallationDate, sensor.Description, sensor.IsActive,
            sensor.CreatedAt, sensor.Source);

    private static bool Valid(params string[] values) =>
        values.All(value => !string.IsNullOrWhiteSpace(value));

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [HttpGet("{id:int}/readings")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SensorReadingResponse>>>
        GetReadings(
            int id,
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            return BadRequest(new
            {
                message = "Limit must be between 1 and 500."
            });
        }

        var sensorExists = await _dbContext.Sensors
            .AnyAsync(sensor => sensor.Id == id, cancellationToken);

        if (!sensorExists)
        {
            return NotFound(new { message = "Sensor not found." });
        }

        var readings = await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(reading => reading.SensorId == id)
            .OrderByDescending(reading => reading.RecordedAt)
            .Take(limit)
            .Select(reading => new SensorReadingResponse(
                reading.Id,
                reading.SensorId,
                reading.Value,
                reading.RecordedAt,
                reading.Source))
            .ToListAsync(cancellationToken);

        return Ok(readings);
    }

    [HttpPost("readings")]
    [AllowAnonymous]
    public async Task<ActionResult<SensorReadingIngestResponse>> IngestHumidity(
        [FromBody] IngestHumidityRequest request,
        CancellationToken cancellationToken)
    {
        var deviceApiKey =
            _configuration["SensorIngestion:ApiKey"];

        var providedDeviceKey =
            Request.Headers["X-Device-Key"].ToString();

        if (string.IsNullOrWhiteSpace(deviceApiKey) ||
            string.IsNullOrWhiteSpace(providedDeviceKey) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(deviceApiKey),
                Encoding.UTF8.GetBytes(providedDeviceKey)))
        {
            return Unauthorized(new { message = "Invalid device key." });
        }

        var sensor = await _dbContext.Sensors
            .SingleOrDefaultAsync(
                item => item.Code == request.SensorCode,
                cancellationToken);

        if (sensor is null)
        {
            return NotFound(new { message = "Sensor not found." });
        }

        if (!sensor.IsActive)
        {
            return BadRequest(new { message = "Sensor is inactive." });
        }

        if (!string.Equals(
                sensor.Type,
                "HUMIDITY",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "This endpoint only accepts humidity readings."
            });
        }

        var reading = new SensorReading
        {
            SensorId = sensor.Id,
            Value = request.Value,
            RecordedAt = DateTime.UtcNow,
            Source = "REAL"
        };

        sensor.Source = "REAL";

        _dbContext.SensorReadings.Add(reading);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var alertServiceApiKey =
            _configuration["AlertService:ApiKey"];

        var alertRequest = new AlertEvaluationRequest(
            sensor.Id,
            sensor.Code,
            sensor.CommunityId,
            sensor.Type,
            reading.Value,
            reading.RecordedAt);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "internal/alerts/evaluate");

        httpRequest.Headers.Add(
            "X-Service-Key",
            alertServiceApiKey);

        httpRequest.Content = JsonContent.Create(alertRequest);

        try
        {
            var alertServiceClient =
                _httpClientFactory.CreateClient("AlertService");

            using var alertResponse = await alertServiceClient.SendAsync(
                httpRequest,
                cancellationToken);

            if (!alertResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AlertService returned HTTP {StatusCode} for reading {ReadingId}.",
                    (int)alertResponse.StatusCode,
                    reading.Id);

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message = "The reading was saved, but alert evaluation failed.",
                        readingId = reading.Id
                    });
            }

            var alertEvaluation =
                await alertResponse.Content
                    .ReadFromJsonAsync<AlertEvaluationResponse>(
                        cancellationToken);

            if (alertEvaluation is null)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message = "AlertService returned an empty response.",
                        readingId = reading.Id
                    });
            }

            return Ok(new SensorReadingIngestResponse(
                reading.Id,
                reading.SensorId,
                reading.Value,
                reading.RecordedAt,
                reading.Source,
                alertEvaluation));
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Could not reach AlertService for reading {ReadingId}.",
                reading.Id);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "The reading was saved, but AlertService could not be reached.",
                    readingId = reading.Id
                });
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "AlertService timed out for reading {ReadingId}.",
                reading.Id);

            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new
                {
                    message = "The reading was saved, but AlertService timed out.",
                    readingId = reading.Id
                });
        }
    }
}

public sealed record IngestHumidityRequest(
    [Required] string SensorCode,
    [Range(typeof(decimal), "0", "100")] decimal Value);

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
    DateTime CreatedAt,
    string Source);

public sealed record CreateSensorRequest(
    int CommunityId, string Code, string Name, string Type, string Unit,
    string? Location, DateTime? InstallationDate, string? Description,
    bool IsActive = true);

public sealed record UpdateSensorRequest(
    int CommunityId, string Code, string Name, string Type, string Unit,
    string? Location, DateTime? InstallationDate, string? Description);

public sealed record ChangeSensorStatusRequest(bool IsActive);

public sealed record CommunityLookup(int Id, string Name);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items, int Page, int PageSize,
    int TotalItems, int TotalPages);

public sealed record SensorReadingResponse(
    long Id,
    int SensorId,
    decimal Value,
    DateTime RecordedAt,
    string Source);

public sealed record SensorReadingIngestResponse(
    long Id,
    int SensorId,
    decimal Value,
    DateTime RecordedAt,
    string Source,
    AlertEvaluationResponse AlertEvaluation);

public sealed record AlertEvaluationRequest(
    int SensorId,
    string SensorCode,
    int CommunityId,
    string SensorType,
    decimal Value,
    DateTime RecordedAt);

public sealed record AlertEvaluationResponse(
    string Outcome,
    bool Active,
    long? AlertId,
    string? RiskLevel,
    string? Phenomenon,
    string? Message,
    decimal? DetectedValue,
    decimal? ThresholdValue,
    bool NotificationRequired);
