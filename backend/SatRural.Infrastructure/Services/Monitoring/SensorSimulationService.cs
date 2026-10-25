using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SatRural.Application.Modules.Monitoring.Interfaces;
using SatRural.Application.Modules.Monitoring.Services;

using SatRural.Domain.Common;
using SatRural.Domain.Entities;

using SatRural.Infrastructure.Persistence;

namespace SatRural.Infrastructure.Services.Monitoring;

public class SensorSimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMonitoringNotifier _notifier;
    private readonly SimulationState _simulationState;

    private readonly Dictionary<int, string> _lastAlertKeys = new();

    private readonly Dictionary<string, decimal> _currentValues =
        new()
        {
            ["TEMPERATURE"] = 27.4m,
            ["HUMIDITY"] = 74m,
            ["WIND_SPEED"] = 16m,
            ["RAINFALL"] = 3.2m,
            ["RIVER_LEVEL"] = 42m
        };

    private readonly Dictionary<string, decimal> _normalValues =
        new()
        {
            ["TEMPERATURE"] = 27.4m,
            ["HUMIDITY"] = 74m,
            ["WIND_SPEED"] = 16m,
            ["RAINFALL"] = 3.2m,
            ["RIVER_LEVEL"] = 42m
        };

    public SensorSimulationService(
        IServiceScopeFactory scopeFactory,
        IMonitoringNotifier notifier,
        SimulationState simulationState)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _simulationState = simulationState;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await GenerateReadingsAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken
            );
        }
    }

    private async Task GenerateReadingsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var riskEvaluationService =
            scope.ServiceProvider
                .GetRequiredService<RiskEvaluationService>();

        var sensors = await dbContext.Sensors
            .Where(sensor => sensor.IsActive)
            .ToListAsync(cancellationToken);

        var activeSensorIds = sensors
            .Select(sensor => sensor.Id)
            .ToHashSet();

        foreach (var sensorId in _lastAlertKeys.Keys
            .Where(id => !activeSensorIds.Contains(id))
            .ToArray())
        {
            _lastAlertKeys.Remove(sensorId);
        }

        foreach (var sensor in sensors)
        {
            decimal value;

            if (_simulationState.TryGetSensorValue(
                sensor.Id,
                out var manualValue))
            {
                value = decimal.Round(
                    manualValue,
                    2
                );

                /*
                 * Si el tipo de sensor forma parte de los valores
                 * utilizados por el dashboard, actualizamos también
                 * su valor actual.
                 */
                if (_currentValues.ContainsKey(sensor.Type))
                {
                    _currentValues[sensor.Type] = value;
                }
            }
            else
            {
                value = GenerateNextValue(
                    sensor.Type,
                    _simulationState.Scenario
                );
            }

            var reading = new SensorReading
            {
                SensorId = sensor.Id,
                Value = value,
                RecordedAt = DateTime.UtcNow
            };

            dbContext.SensorReadings.Add(reading);

            var risk = await riskEvaluationService.EvaluateSensorAsync(
                sensor.Type,
                value,
                cancellationToken
            );

            if (risk.Level == RiskLevel.Green)
            {
                _lastAlertKeys.Remove(sensor.Id);
                continue;
            }

            var alertKey =
                $"{risk.AlertRuleId}:{risk.Level}:{risk.Message}";

            if (_lastAlertKeys.TryGetValue(
                    sensor.Id,
                    out var previousAlertKey) &&
                previousAlertKey == alertKey)
            {
                continue;
            }

            var alert = new Alert
            {
                CommunityId = sensor.CommunityId,
                SensorId = sensor.Id,
                Type = risk.Phenomenon,
                Level = risk.Level
                    .ToString()
                    .ToUpperInvariant(),
                Message = risk.Message,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AlertRuleId = risk.AlertRuleId,
                DetectedValue = risk.DetectedValue,
                ThresholdValue = risk.ThresholdValue
            };

            dbContext.Alerts.Add(alert);

            var eventItem = new Event
            {
                Alert = alert,
                CommunityId = alert.CommunityId,
                EventType = risk.Phenomenon,
                Description = risk.Message,
                OccurredAt = alert.CreatedAt
            };

            dbContext.Events.Add(eventItem);

            _lastAlertKeys[sensor.Id] = alertKey;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken
        );

        await _notifier.NotifyReadingsUpdatedAsync(
            cancellationToken
        );
    }

    private decimal GenerateNextValue(
        string sensorType,
        string scenario)
    {
        var currentValue =
            _currentValues.TryGetValue(
                sensorType,
                out var value
            )
                ? value
                : 0;

        decimal variation;

        switch (scenario)
        {
            case "HEAVY_RAIN":

                variation = sensorType switch
                {
                    "RAINFALL" =>
                        RandomDecimal(2m, 5m),

                    "HUMIDITY" =>
                        RandomDecimal(0.5m, 2m),

                    "RIVER_LEVEL" =>
                        RandomDecimal(0.5m, 2m),

                    _ =>
                        RandomDecimal(-0.3m, 0.3m)
                };

                break;

            case "STORM":

                variation = sensorType switch
                {
                    "RAINFALL" =>
                        RandomDecimal(3m, 6m),

                    "WIND_SPEED" =>
                        RandomDecimal(4m, 8m),

                    "HUMIDITY" =>
                        RandomDecimal(1m, 3m),

                    _ =>
                        RandomDecimal(-0.3m, 0.3m)
                };

                break;

            case "FLOOD":

                variation = sensorType switch
                {
                    "RAINFALL" =>
                        RandomDecimal(4m, 7m),

                    "RIVER_LEVEL" =>
                        RandomDecimal(4m, 8m),

                    "HUMIDITY" =>
                        RandomDecimal(1m, 2m),

                    _ =>
                        RandomDecimal(-0.2m, 0.2m)
                };

                break;

            case "DROUGHT":

                variation = sensorType switch
                {
                    "TEMPERATURE" =>
                        RandomDecimal(0.5m, 1.5m),

                    "HUMIDITY" =>
                        RandomDecimal(-4m, -2m),

                    "RAINFALL" =>
                        RandomDecimal(-2m, -1m),

                    "RIVER_LEVEL" =>
                        RandomDecimal(-2m, -1m),

                    _ => 0
                };

                break;

            case "FROST":

                variation = sensorType switch
                {
                    "TEMPERATURE" =>
                        RandomDecimal(-5m, -2m),

                    _ =>
                        RandomDecimal(-0.2m, 0.2m)
                };

                break;

            case "FOREST_FIRE":

                variation = sensorType switch
                {
                    "TEMPERATURE" =>
                        RandomDecimal(1m, 2m),

                    "HUMIDITY" =>
                        RandomDecimal(-5m, -2m),

                    "WIND_SPEED" =>
                        RandomDecimal(1m, 4m),

                    "RAINFALL" =>
                        RandomDecimal(-2m, -1m),

                    _ => 0
                };

                break;

            default:

                var targetValue =
                    _normalValues.TryGetValue(
                        sensorType,
                        out var normalValue
                    )
                        ? normalValue
                        : currentValue;

                var difference =
                    targetValue - currentValue;

                variation = sensorType switch
                {
                    "TEMPERATURE" =>
                        difference * 0.25m +
                        RandomDecimal(-0.15m, 0.15m),

                    "HUMIDITY" =>
                        difference * 0.25m +
                        RandomDecimal(-0.4m, 0.4m),

                    "WIND_SPEED" =>
                        difference * 0.30m +
                        RandomDecimal(-0.5m, 0.5m),

                    "RAINFALL" =>
                        difference * 0.35m +
                        RandomDecimal(-0.15m, 0.15m),

                    "RIVER_LEVEL" =>
                        difference * 0.20m +
                        RandomDecimal(-0.3m, 0.3m),

                    _ => 0
                };

                break;
        }

        var newValue =
            currentValue + variation;

        if (
            scenario == "NORMAL" &&
            _normalValues.TryGetValue(
                sensorType,
                out var normalTarget
            ) &&
            Math.Abs(newValue - normalTarget) < 0.5m
        )
        {
            newValue =
                normalTarget +
                RandomDecimal(-0.15m, 0.15m);
        }

        newValue = sensorType switch
        {
            "TEMPERATURE" =>
                Clamp(newValue, -5m, 45m),

            "HUMIDITY" =>
                Clamp(newValue, 10m, 100m),

            "WIND_SPEED" =>
                Clamp(newValue, 0m, 100m),

            "RAINFALL" =>
                Clamp(newValue, 0m, 70m),

            "RIVER_LEVEL" =>
                Clamp(newValue, 5m, 100m),

            _ => newValue
        };

        newValue = decimal.Round(
            newValue,
            2
        );

        _currentValues[sensorType] = newValue;

        return newValue;
    }

    private static decimal RandomDecimal(
        decimal min,
        decimal max)
    {
        var value =
            Random.Shared.NextDouble();

        return min +
            (decimal)value * (max - min);
    }

    private static decimal Clamp(
        decimal value,
        decimal min,
        decimal max)
    {
        return Math.Min(
            Math.Max(value, min),
            max
        );
    }
}