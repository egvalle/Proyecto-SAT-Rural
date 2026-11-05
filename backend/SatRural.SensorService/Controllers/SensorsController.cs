using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Entities;
using SatRural.SensorService.Persistence;

namespace SatRural.SensorService.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly SensorDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SensorsController> _logger;

    public SensorsController(
        SensorDbContext dbContext,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SensorsController> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SensorResponse>>> GetSensors(
        CancellationToken cancellationToken)
    {
        var sensors = await _dbContext.Sensors
            .AsNoTracking()
            .OrderBy(sensor => sensor.Id)
            .Select(sensor => new SensorResponse(
                sensor.Id,
                sensor.CommunityId,
                sensor.Code,
                sensor.Name,
                sensor.Type,
                sensor.Unit,
                sensor.IsActive,
                sensor.Source))
            .ToListAsync(cancellationToken);

        return Ok(sensors);
    }

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
    string Code,
    string Name,
    string Type,
    string Unit,
    bool IsActive,
    string Source);

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