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

    public MonitoringController(AppDbContext dbContext, RiskEvaluationService riskEvaluationService)
    {
        _dbContext = dbContext;
        _riskEvaluationService = riskEvaluationService;
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
}