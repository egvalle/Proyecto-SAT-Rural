using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SatRural.Application.Modules.Monitoring;
using SatRural.Application.Modules.Monitoring.Services;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly RiskEvaluationService _riskEvaluationService;
    private readonly SimulationState _simulationState;

    public MonitoringController(AppDbContext dbContext, RiskEvaluationService riskEvaluationService, SimulationState simulationState)
    {
        _dbContext = dbContext;
        _riskEvaluationService = riskEvaluationService;
        _simulationState = simulationState;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var latestReadings = await _dbContext.Sensors
            .AsNoTracking()
            .Where(sensor => sensor.IsActive)
            .Select(sensor => new
            {
                sensor.Type,

                Value = sensor.Readings
                    .OrderByDescending(reading => reading.RecordedAt)
                    .Select(reading => (decimal?)reading.Value)
                    .FirstOrDefault(),

                RecordedAt = sensor.Readings
                    .OrderByDescending(reading => reading.RecordedAt)
                    .Select(reading => (DateTime?)reading.RecordedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        decimal GetValue(string type)
        {
            return latestReadings
                .FirstOrDefault(x => x.Type == type)
                ?.Value ?? 0;
        }

        var temperature = GetValue("TEMPERATURE");
        var humidity = GetValue("HUMIDITY");
        var windSpeed = GetValue("WIND_SPEED");
        var rainfall = GetValue("RAINFALL");
        var riverLevel = GetValue("RIVER_LEVEL");

        var updatedAt = latestReadings
            .Where(x => x.RecordedAt.HasValue)
            .Select(x => x.RecordedAt!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        var risk = _riskEvaluationService.Evaluate(
            temperature,
            humidity,
            windSpeed,
            rainfall,
            riverLevel
        );

        var dashboard = new DashboardDto
        {
            Temperature = temperature,
            Humidity = humidity,
            WindSpeed = windSpeed,
            Rainfall = rainfall,
            RiverLevel = riverLevel,

            Status = risk.Level.ToString().ToUpperInvariant(),

            StatusMessage = risk.Message,

            UpdatedAt = updatedAt
        };

        return Ok(dashboard);
    }

    [HttpPost("simulation")]
    public IActionResult StartSimulation(
        [FromBody] SimulationRequestDto request)
    {
        var validScenarios = new[]
        {
            "NORMAL",
            "HEAVY_RAIN",
            "STORM",
            "FLOOD",
            "DROUGHT",
            "FROST",
            "FOREST_FIRE"
        };

        var scenario = request.Scenario.ToUpperInvariant();

        if (!validScenarios.Contains(scenario))
        {
            return BadRequest(new
            {
                message = "Escenario no válido."
            });
        }

        _simulationState.SetScenario(scenario);

        return Ok(new
        {
            scenario,
            message = "Escenario activado correctamente."
        });
    }

    [HttpPost("simulation/reset")]
    public IActionResult ResetSimulation()
    {
        _simulationState.Reset();

        return Ok(new
        {
            scenario = "NORMAL",
            message = "Simulación reiniciada."
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 5, 100);

        var sensors = await _dbContext.Sensors
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                (
                    x.Type == "TEMPERATURE" ||
                    x.Type == "RAINFALL" ||
                    x.Type == "RIVER_LEVEL"
                )
            )
            .Select(sensor => new
            {
                sensor.Type,

                Readings = sensor.Readings
                    .OrderByDescending(x => x.RecordedAt)
                    .Take(limit)
                    .OrderBy(x => x.RecordedAt)
                    .Select(x => new
                    {
                        x.Value,
                        x.RecordedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(sensors);
    }

    [HttpGet("alerts/recent")]
    public async Task<IActionResult> GetRecentAlerts(
        [FromQuery] int limit = 5)
    {
        limit = Math.Clamp(limit, 1, 20);

        var alerts = await _dbContext.Alerts
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Level,
                x.Message,
                x.CreatedAt,
                x.IsActive
            })
            .ToListAsync();

        return Ok(alerts);
    }
}