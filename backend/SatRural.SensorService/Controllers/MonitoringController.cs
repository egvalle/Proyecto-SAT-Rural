using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Persistence;

namespace SatRural.SensorService.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    private static readonly string[] Scenarios =
    [
        "NORMAL", "HEAVY_RAIN", "STORM", "FLOOD", "DROUGHT", "FROST", "FOREST_FIRE"
    ];

    private readonly SensorDbContext _db;
    private readonly IHttpClientFactory _clients;

    public MonitoringController(SensorDbContext db, IHttpClientFactory clients)
    {
        _db = db;
        _clients = clients;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var sensors = await _db.Sensors.AsNoTracking()
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var latest = new Dictionary<string, decimal>();
        var updatedAt = DateTime.MinValue;
        foreach (var sensor in sensors)
        {
            var reading = await _db.SensorReadings.AsNoTracking()
                .Where(item => item.SensorId == sensor.Id)
                .OrderByDescending(item => item.RecordedAt)
                .Select(item => new { item.Value, item.RecordedAt })
                .FirstOrDefaultAsync(cancellationToken);
            if (reading is null) continue;
            latest[sensor.Type] = reading.Value;
            if (reading.RecordedAt > updatedAt) updatedAt = reading.RecordedAt;
        }

        var alerts = await GetAlertsAsync(true, 100, cancellationToken);
        if (alerts is null) return StatusCode(502,
            new { message = "No se pudo consultar el estado de alertas." });
        var highest = alerts.OrderByDescending(item => Severity(item.Level)).FirstOrDefault();
        var status = highest?.Level ?? "GREEN";
        return Ok(new
        {
            temperature = latest.GetValueOrDefault("TEMPERATURE"),
            humidity = latest.GetValueOrDefault("HUMIDITY"),
            windSpeed = latest.GetValueOrDefault("WIND_SPEED"),
            rainfall = latest.GetValueOrDefault("RAINFALL"),
            riverLevel = latest.GetValueOrDefault("RIVER_LEVEL"),
            status,
            statusMessage = highest is null
                ? "No se detectan condiciones críticas en la comunidad."
                : highest.Message,
            updatedAt = updatedAt == DateTime.MinValue ? DateTime.UtcNow : updatedAt
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 5, 100);
        var sensors = await _db.Sensors.AsNoTracking()
            .Where(item => item.IsActive &&
                (item.Type == "TEMPERATURE" || item.Type == "RAINFALL" ||
                 item.Type == "RIVER_LEVEL"))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var history = new List<object>();
        foreach (var sensor in sensors)
        {
            var readings = await _db.SensorReadings.AsNoTracking()
                .Where(item => item.SensorId == sensor.Id)
                .OrderByDescending(item => item.RecordedAt)
                .Take(limit)
                .Select(item => new { item.Value, item.RecordedAt })
                .ToListAsync(cancellationToken);
            readings.Reverse();
            history.Add(new { sensor.Type, Readings = readings });
        }
        return Ok(history);
    }

    [HttpGet("alerts/recent")]
    public async Task<IActionResult> RecentAlerts(
        [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        var alerts = await GetAlertsAsync(null, Math.Clamp(limit, 1, 20), cancellationToken);
        if (alerts is null) return StatusCode(502,
            new { message = "No se pudo consultar las alertas recientes." });
        return Ok(alerts.Select(item => new
        {
            item.Id,
            Type = item.Type,
            item.Level,
            item.Message,
            item.CreatedAt,
            item.IsActive
        }));
    }

    [HttpPost("simulation")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<IActionResult> StartSimulation(
        SimulationRequest request, CancellationToken cancellationToken)
    {
        var scenario = request.Scenario?.Trim().ToUpperInvariant();
        if (scenario is null || !Scenarios.Contains(scenario))
            return BadRequest(new { message = "Escenario no válido." });
        var setting = await _db.SimulationSettings
            .SingleAsync(item => item.Id == 1, cancellationToken);
        setting.Scenario = scenario;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { scenario, message = "Escenario activado correctamente." });
    }

    [HttpPost("simulation/reset")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<IActionResult> ResetSimulation(CancellationToken cancellationToken)
    {
        var setting = await _db.SimulationSettings
            .SingleAsync(item => item.Id == 1, cancellationToken);
        setting.Scenario = "NORMAL";
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { scenario = "NORMAL", message = "Simulación reiniciada." });
    }

    [HttpPatch("simulation/sensors/{sensorId:int}")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<IActionResult> SetSensorValue(
        int sensorId, SensorSimulationValueRequest request,
        CancellationToken cancellationToken)
    {
        var sensor = await _db.Sensors.SingleOrDefaultAsync(
            item => item.Id == sensorId, cancellationToken);
        if (sensor is null) return NotFound(new { message = "Sensor no encontrado." });
        if (!sensor.IsActive || sensor.Source == "REAL")
            return BadRequest(new { message = "Este sensor no admite simulación." });
        if (!ValidValue(sensor.Type, request.Value))
            return BadRequest(new { message = "Valor fuera del rango del sensor." });
        sensor.ManualSimulationValue = decimal.Round(request.Value, 2);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            sensorId, sensorCode = sensor.Code, sensorName = sensor.Name,
            sensorType = sensor.Type, value = sensor.ManualSimulationValue,
            unit = sensor.Unit, mode = "MANUAL",
            message = "Valor simulado actualizado correctamente."
        });
    }

    [HttpDelete("simulation/sensors/{sensorId:int}")]
    [Authorize(Roles = "ADMIN,OPERATOR")]
    public async Task<IActionResult> ResetSensorValue(
        int sensorId, CancellationToken cancellationToken)
    {
        var sensor = await _db.Sensors.SingleOrDefaultAsync(
            item => item.Id == sensorId, cancellationToken);
        if (sensor is null) return NotFound(new { message = "Sensor no encontrado." });
        sensor.ManualSimulationValue = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            sensorId, sensorCode = sensor.Code, mode = "AUTOMATIC",
            message = "El sensor volvió al modo de simulación automática."
        });
    }

    private async Task<List<MonitoringAlert>?> GetAlertsAsync(
        bool? active, int limit, CancellationToken cancellationToken)
    {
        var suffix = active.HasValue ? $"&isActive={active.Value.ToString().ToLowerInvariant()}" : "";
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/alerts?page=1&pageSize={limit}{suffix}");
        if (AuthenticationHeaderValue.TryParse(
            Request.Headers.Authorization.ToString(), out var authorization))
            request.Headers.Authorization = authorization;
        try
        {
            using var response = await _clients.CreateClient("AlertService")
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<List<MonitoringAlert>>(
                cancellationToken);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
    }

    private static int Severity(string level) => level switch
    {
        "RED" => 3,
        "ORANGE" => 2,
        "YELLOW" => 1,
        _ => 0
    };

    private static bool ValidValue(string type, decimal value) => type switch
    {
        "TEMPERATURE" => value is >= -5m and <= 45m,
        "HUMIDITY" or "RIVER_LEVEL" => value is >= 0m and <= 100m,
        "WIND_SPEED" => value is >= 0m and <= 100m,
        "RAINFALL" => value is >= 0m and <= 70m,
        _ => true
    };
}

public sealed record SimulationRequest(string? Scenario);
public sealed record SensorSimulationValueRequest(decimal Value);
public sealed record MonitoringAlert(
    long Id, string Type, string Level, string Message,
    DateTime CreatedAt, bool IsActive);
