using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Entities;
using SatRural.SensorService.Persistence;

namespace SatRural.SensorService.Services;

public sealed class SensorSimulationService : BackgroundService
{
    private static readonly Dictionary<string, decimal> NormalValues = new()
    {
        ["TEMPERATURE"] = 27.4m,
        ["HUMIDITY"] = 55m,
        ["WIND_SPEED"] = 16m,
        ["RAINFALL"] = 3.2m,
        ["RIVER_LEVEL"] = 42m
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SensorSimulationService> _logger;

    public SensorSimulationService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory clients,
        IConfiguration configuration,
        ILogger<SensorSimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _clients = clients;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await GenerateReadingsAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Could not generate simulated readings.");
            }
        }
    }

    private async Task GenerateReadingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
        var scenario = await db.SimulationSettings.AsNoTracking()
            .Where(item => item.Id == 1)
            .Select(item => item.Scenario)
            .FirstAsync(cancellationToken);
        var sensors = await db.Sensors
            .Where(item => item.IsActive && item.Source != "REAL")
            .ToListAsync(cancellationToken);
        var client = _clients.CreateClient("AlertService");

        foreach (var sensor in sensors)
        {
            var previous = await db.SensorReadings.AsNoTracking()
                .Where(item => item.SensorId == sensor.Id)
                .OrderByDescending(item => item.RecordedAt)
                .Select(item => (decimal?)item.Value)
                .FirstOrDefaultAsync(cancellationToken);
            var value = sensor.ManualSimulationValue ??
                NextValue(sensor.Type, scenario, previous);
            var reading = new SensorReading
            {
                SensorId = sensor.Id,
                Value = value,
                RecordedAt = DateTime.UtcNow,
                Source = "SIMULATED"
            };
            db.SensorReadings.Add(reading);
            await db.SaveChangesAsync(cancellationToken);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "internal/alerts/evaluate");
            request.Headers.Add("X-Service-Key", _configuration["AlertService:ApiKey"]);
            request.Content = JsonContent.Create(new
            {
                sensorId = sensor.Id,
                sensorCode = sensor.Code,
                communityId = sensor.CommunityId,
                sensorType = sensor.Type,
                value,
                recordedAt = reading.RecordedAt
            });
            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("AlertService returned {StatusCode} for sensor {SensorId}.",
                        (int)response.StatusCode, sensor.Id);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception,
                    "Could not evaluate simulated reading for sensor {SensorId}.", sensor.Id);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception,
                    "Alert evaluation timed out for sensor {SensorId}.", sensor.Id);
            }
        }
    }

    private static decimal NextValue(string type, string scenario, decimal? previous)
    {
        var normal = NormalValues.GetValueOrDefault(type, 0m);
        var current = previous ?? normal;
        var movement = scenario switch
        {
            "HEAVY_RAIN" => type switch
            {
                "RAINFALL" => RandomDelta(2m, 5m),
                "RIVER_LEVEL" => RandomDelta(0.5m, 2m),
                "HUMIDITY" => RandomDelta(0.5m, 2m),
                _ => RandomDelta(-0.3m, 0.3m)
            },
            "STORM" => type switch
            {
                "RAINFALL" => RandomDelta(3m, 6m),
                "WIND_SPEED" => RandomDelta(4m, 8m),
                "HUMIDITY" => RandomDelta(1m, 3m),
                _ => RandomDelta(-0.3m, 0.3m)
            },
            "FLOOD" => type switch
            {
                "RAINFALL" => RandomDelta(4m, 7m),
                "RIVER_LEVEL" => RandomDelta(4m, 8m),
                _ => RandomDelta(-0.2m, 0.2m)
            },
            "DROUGHT" => type switch
            {
                "TEMPERATURE" => RandomDelta(0.5m, 1.5m),
                "HUMIDITY" => RandomDelta(-4m, -2m),
                "RAINFALL" or "RIVER_LEVEL" => RandomDelta(-2m, -1m),
                _ => 0m
            },
            "FROST" => type == "TEMPERATURE"
                ? RandomDelta(-5m, -2m) : RandomDelta(-0.2m, 0.2m),
            "FOREST_FIRE" => type switch
            {
                "TEMPERATURE" => RandomDelta(1m, 2m),
                "HUMIDITY" => RandomDelta(-5m, -2m),
                "WIND_SPEED" => RandomDelta(1m, 4m),
                "RAINFALL" => RandomDelta(-2m, -1m),
                _ => 0m
            },
            _ => (normal - current) * 0.3m + RandomDelta(-0.3m, 0.3m)
        };
        var next = current + movement;
        var (minimum, maximum) = type switch
        {
            "TEMPERATURE" => (-5m, 45m),
            "HUMIDITY" => (0m, 100m),
            "WIND_SPEED" => (0m, 100m),
            "RAINFALL" => (0m, 70m),
            "RIVER_LEVEL" => (0m, 100m),
            _ => (decimal.MinValue, decimal.MaxValue)
        };
        return decimal.Round(Math.Clamp(next, minimum, maximum), 2);
    }

    private static decimal RandomDelta(decimal minimum, decimal maximum) =>
        minimum + (decimal)Random.Shared.NextDouble() * (maximum - minimum);
}
