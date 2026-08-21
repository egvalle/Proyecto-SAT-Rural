using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SatRural.Application.Modules.Monitoring.Interfaces;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Infrastructure.Services.Monitoring;

public class SensorSimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMonitoringNotifier _notifier;

    private readonly Dictionary<string, decimal> _currentValues =
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
        IMonitoringNotifier notifier)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
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

        var sensors = await dbContext.Sensors
            .Where(sensor => sensor.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var sensor in sensors)
        {
            var value = GenerateNextValue(sensor.Type);

            var reading = new SensorReading
            {
                SensorId = sensor.Id,
                Value = value,
                RecordedAt = DateTime.UtcNow
            };

            dbContext.SensorReadings.Add(reading);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Avisar a los clientes conectados mediante SignalR
        await _notifier.NotifyReadingsUpdatedAsync(
            cancellationToken
        );
    }

    private decimal GenerateNextValue(string sensorType)
    {
        if (!_currentValues.TryGetValue(
                sensorType,
                out var currentValue))
        {
            return 0;
        }

        var variation = sensorType switch
        {
            "TEMPERATURE" =>
                RandomDecimal(-0.4m, 0.4m),

            "HUMIDITY" =>
                RandomDecimal(-1.5m, 1.5m),

            "WIND_SPEED" =>
                RandomDecimal(-2m, 2m),

            "RAINFALL" =>
                RandomDecimal(-0.8m, 0.8m),

            "RIVER_LEVEL" =>
                RandomDecimal(-1m, 1m),

            _ => 0
        };

        var newValue = currentValue + variation;

        newValue = sensorType switch
        {
            "TEMPERATURE" =>
                Clamp(newValue, 18m, 35m),

            "HUMIDITY" =>
                Clamp(newValue, 40m, 95m),

            "WIND_SPEED" =>
                Clamp(newValue, 0m, 50m),

            "RAINFALL" =>
                Clamp(newValue, 0m, 30m),

            "RIVER_LEVEL" =>
                Clamp(newValue, 20m, 90m),

            _ => newValue
        };

        newValue = decimal.Round(newValue, 2);

        _currentValues[sensorType] = newValue;

        return newValue;
    }

    private static decimal RandomDecimal(
        decimal min,
        decimal max)
    {
        var value = Random.Shared.NextDouble();

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