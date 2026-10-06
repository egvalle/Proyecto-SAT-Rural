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

    public MonitoringController(
        AppDbContext dbContext,
        RiskEvaluationService riskEvaluationService,
        SimulationState simulationState)
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

            Status = risk.Level
                .ToString()
                .ToUpperInvariant(),

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

        var scenario =
            request.Scenario.ToUpperInvariant();

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

    /*
     * Permite establecer manualmente el valor simulado
     * de un sensor específico.
     */
    [HttpPatch("simulation/sensors/{sensorId:int}")]
    public async Task<IActionResult> SetSensorSimulationValue(
        int sensorId,
        [FromBody] SensorSimulationValueRequest request)
    {
        var sensor = await _dbContext.Sensors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == sensorId
            );

        if (sensor is null)
        {
            return NotFound(new
            {
                message = "Sensor no encontrado."
            });
        }

        if (!sensor.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "No se puede modificar la simulación de un sensor inactivo."
            });
        }

        if (!IsValidSensorValue(
            sensor.Type,
            request.Value))
        {
            return BadRequest(new
            {
                message =
                    GetSensorValueValidationMessage(
                        sensor.Type
                    )
            });
        }

        _simulationState.SetSensorValue(
            sensor.Id,
            request.Value
        );

        return Ok(new
        {
            sensorId = sensor.Id,
            sensorCode = sensor.Code,
            sensorName = sensor.Name,
            sensorType = sensor.Type,
            value = decimal.Round(
                request.Value,
                2
            ),
            unit = sensor.Unit,
            mode = "MANUAL",
            message =
                "Valor simulado actualizado correctamente."
        });
    }

    /*
     * Elimina el valor manual de un sensor para que
     * vuelva a utilizar la simulación automática.
     */
    [HttpDelete("simulation/sensors/{sensorId:int}")]
    public async Task<IActionResult> ResetSensorSimulationValue(
        int sensorId)
    {
        var sensor = await _dbContext.Sensors
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == sensorId
            );

        if (sensor is null)
        {
            return NotFound(new
            {
                message = "Sensor no encontrado."
            });
        }

        _simulationState.RemoveSensorValue(
            sensor.Id
        );

        return Ok(new
        {
            sensorId = sensor.Id,
            sensorCode = sensor.Code,
            mode = "AUTOMATIC",
            message =
                "El sensor volvió al modo de simulación automática."
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(
            limit,
            5,
            100
        );

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
                    .OrderByDescending(
                        x => x.RecordedAt
                    )
                    .Take(limit)
                    .OrderBy(
                        x => x.RecordedAt
                    )
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
        limit = Math.Clamp(
            limit,
            1,
            20
        );

        var alerts = await _dbContext.Alerts
            .AsNoTracking()
            .OrderByDescending(
                x => x.CreatedAt
            )
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

    private static bool IsValidSensorValue(
        string sensorType,
        decimal value)
    {
        return sensorType switch
        {
            "TEMPERATURE" =>
                value >= -5m &&
                value <= 45m,

            "HUMIDITY" =>
                value >= 0m &&
                value <= 100m,

            "WIND_SPEED" =>
                value >= 0m &&
                value <= 100m,

            "RAINFALL" =>
                value >= 0m &&
                value <= 70m,

            "RIVER_LEVEL" =>
                value >= 0m &&
                value <= 100m,

            "RESERVOIR_LEVEL" =>
                value >= 0m &&
                value <= 100m,

            "SMOKE_FIRE" =>
                value >= 0m,

            "OTHER_ENVIRONMENTAL" =>
                true,

            _ =>
                true
        };
    }

    private static string GetSensorValueValidationMessage(
        string sensorType)
    {
        return sensorType switch
        {
            "TEMPERATURE" =>
                "La temperatura debe estar entre -5 y 45.",

            "HUMIDITY" =>
                "La humedad debe estar entre 0 y 100.",

            "WIND_SPEED" =>
                "La velocidad del viento debe estar entre 0 y 100.",

            "RAINFALL" =>
                "La lluvia debe estar entre 0 y 70.",

            "RIVER_LEVEL" =>
                "El nivel del río debe estar entre 0 y 100.",

            "RESERVOIR_LEVEL" =>
                "El nivel del embalse debe estar entre 0 y 100.",

            "SMOKE_FIRE" =>
                "El valor de humo/incendio no puede ser negativo.",

            _ =>
                "El valor indicado no es válido."
        };
    }
}

public sealed record SensorSimulationValueRequest(
    decimal Value
);