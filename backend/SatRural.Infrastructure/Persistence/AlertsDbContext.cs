using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence;

public sealed class AlertsDbContext(DbContextOptions<AlertsDbContext> options)
    : DbContext(options)
{
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var alerts = modelBuilder.Entity<Alert>();
        alerts.ToTable("Alerts");
        alerts.HasKey(alert => alert.Id);
        alerts.Property(alert => alert.DetectedValue).HasPrecision(18, 2);
        alerts.Property(alert => alert.ThresholdValue).HasPrecision(18, 2);
        alerts.Ignore(alert => alert.Community);
        alerts.Ignore(alert => alert.Sensor);
        alerts.Ignore(alert => alert.Events);

        var rules = modelBuilder.Entity<AlertRule>();
        rules.ToTable("AlertRules");
        rules.HasKey(rule => rule.Id);
        rules.Property(rule => rule.SensorType).IsRequired();
        rules.Property(rule => rule.Operator).IsRequired();
        rules.Property(rule => rule.ThresholdValue).HasPrecision(18, 2);
        rules.Property(rule => rule.RiskLevel).IsRequired();
        rules.Property(rule => rule.Phenomenon).IsRequired();
        rules.Property(rule => rule.Description).IsRequired();
        rules.HasData(
            new AlertRule { Id = 1, SensorType = "TEMPERATURE", Operator = ">=", ThresholdValue = 32m, RiskLevel = "YELLOW", Phenomenon = "Ola de calor", Description = "Temperatura elevada" },
            new AlertRule { Id = 2, SensorType = "TEMPERATURE", Operator = ">=", ThresholdValue = 35m, RiskLevel = "ORANGE", Phenomenon = "Ola de calor", Description = "Temperatura muy elevada" },
            new AlertRule { Id = 3, SensorType = "TEMPERATURE", Operator = ">=", ThresholdValue = 38m, RiskLevel = "RED", Phenomenon = "Ola de calor", Description = "Temperatura extremadamente elevada" },
            new AlertRule { Id = 4, SensorType = "TEMPERATURE", Operator = "<=", ThresholdValue = 8m, RiskLevel = "YELLOW", Phenomenon = "Helada", Description = "Temperatura baja" },
            new AlertRule { Id = 5, SensorType = "TEMPERATURE", Operator = "<=", ThresholdValue = 4m, RiskLevel = "ORANGE", Phenomenon = "Helada", Description = "Temperatura muy baja" },
            new AlertRule { Id = 6, SensorType = "TEMPERATURE", Operator = "<=", ThresholdValue = 0m, RiskLevel = "RED", Phenomenon = "Helada", Description = "Temperatura extremadamente baja" },
            new AlertRule { Id = 7, SensorType = "HUMIDITY", Operator = "<=", ThresholdValue = 40m, RiskLevel = "YELLOW", Phenomenon = "Sequía", Description = "Humedad baja" },
            new AlertRule { Id = 8, SensorType = "HUMIDITY", Operator = "<=", ThresholdValue = 30m, RiskLevel = "ORANGE", Phenomenon = "Sequía", Description = "Humedad muy baja" },
            new AlertRule { Id = 9, SensorType = "HUMIDITY", Operator = "<=", ThresholdValue = 20m, RiskLevel = "RED", Phenomenon = "Sequía", Description = "Humedad extremadamente baja" },
            new AlertRule { Id = 10, SensorType = "WIND_SPEED", Operator = ">=", ThresholdValue = 30m, RiskLevel = "YELLOW", Phenomenon = "Tormenta", Description = "Velocidad del viento elevada" },
            new AlertRule { Id = 11, SensorType = "WIND_SPEED", Operator = ">=", ThresholdValue = 50m, RiskLevel = "ORANGE", Phenomenon = "Tormenta", Description = "Velocidad del viento muy elevada" },
            new AlertRule { Id = 12, SensorType = "WIND_SPEED", Operator = ">=", ThresholdValue = 70m, RiskLevel = "RED", Phenomenon = "Tormenta", Description = "Velocidad del viento extremadamente elevada" },
            new AlertRule { Id = 13, SensorType = "RAINFALL", Operator = ">=", ThresholdValue = 15m, RiskLevel = "YELLOW", Phenomenon = "Inundación", Description = "Nivel de lluvia elevado" },
            new AlertRule { Id = 14, SensorType = "RAINFALL", Operator = ">=", ThresholdValue = 30m, RiskLevel = "ORANGE", Phenomenon = "Inundación", Description = "Nivel de lluvia muy elevado" },
            new AlertRule { Id = 15, SensorType = "RAINFALL", Operator = ">=", ThresholdValue = 50m, RiskLevel = "RED", Phenomenon = "Inundación", Description = "Nivel de lluvia extremadamente elevado" },
            new AlertRule { Id = 16, SensorType = "RIVER_LEVEL", Operator = ">=", ThresholdValue = 60m, RiskLevel = "YELLOW", Phenomenon = "Inundación", Description = "Nivel del río elevado" },
            new AlertRule { Id = 17, SensorType = "RIVER_LEVEL", Operator = ">=", ThresholdValue = 75m, RiskLevel = "ORANGE", Phenomenon = "Inundación", Description = "Nivel del río muy elevado" },
            new AlertRule { Id = 18, SensorType = "RIVER_LEVEL", Operator = ">=", ThresholdValue = 90m, RiskLevel = "RED", Phenomenon = "Inundación", Description = "Nivel del río extremadamente elevado" });
    }
}
