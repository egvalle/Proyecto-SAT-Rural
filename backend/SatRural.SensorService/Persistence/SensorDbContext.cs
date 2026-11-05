using Microsoft.EntityFrameworkCore;
using SatRural.SensorService.Entities;

namespace SatRural.SensorService.Persistence;

public sealed class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<SensorReading> SensorReadings =>
        Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var sensor = modelBuilder.Entity<Sensor>();

        sensor.HasKey(item => item.Id);

        sensor.Property(item => item.Code)
            .HasMaxLength(50)
            .IsRequired();

        sensor.HasIndex(item => item.Code)
            .IsUnique();

        sensor.Property(item => item.Name)
            .HasMaxLength(150)
            .IsRequired();

        sensor.Property(item => item.Type)
            .HasMaxLength(50)
            .IsRequired();

        sensor.Property(item => item.Unit)
            .HasMaxLength(20)
            .IsRequired();

        sensor.Property(item => item.Location)
            .HasMaxLength(200);

        sensor.Property(item => item.Description)
            .HasMaxLength(500);

        sensor.Property(item => item.Source)
            .HasMaxLength(20)
            .HasDefaultValue("SIMULATED")
            .IsRequired();

        sensor.HasMany(item => item.Readings)
            .WithOne(reading => reading.Sensor)
            .HasForeignKey(reading => reading.SensorId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedDate = new DateTime(
            2026, 8, 20, 0, 0, 0,
            DateTimeKind.Utc);

        sensor.HasData(
            new Sensor
            {
                Id = 1,
                CommunityId = 1,
                Code = "TEMP-001",
                Name = "Temperatura Ambiente",
                Type = "TEMPERATURE",
                Unit = "°C",
                IsActive = true,
                Source = "SIMULATED",
                CreatedAt = seedDate
            },
            new Sensor
            {
                Id = 2,
                CommunityId = 1,
                Code = "HUM-001",
                Name = "Humedad Relativa",
                Type = "HUMIDITY",
                Unit = "%",
                IsActive = true,
                Source = "SIMULATED",
                CreatedAt = seedDate
            },
            new Sensor
            {
                Id = 3,
                CommunityId = 1,
                Code = "WIND-001",
                Name = "Velocidad del Viento",
                Type = "WIND_SPEED",
                Unit = "km/h",
                IsActive = true,
                Source = "SIMULATED",
                CreatedAt = seedDate
            },
            new Sensor
            {
                Id = 4,
                CommunityId = 1,
                Code = "RAIN-001",
                Name = "Nivel de Lluvia",
                Type = "RAINFALL",
                Unit = "mm/h",
                IsActive = true,
                Source = "SIMULATED",
                CreatedAt = seedDate
            },
            new Sensor
            {
                Id = 5,
                CommunityId = 1,
                Code = "RIVER-001",
                Name = "Nivel del Río",
                Type = "RIVER_LEVEL",
                Unit = "%",
                IsActive = true,
                Source = "SIMULATED",
                CreatedAt = seedDate
            });

        var reading = modelBuilder.Entity<SensorReading>();

        reading.HasKey(item => item.Id);

        reading.Property(item => item.Value)
            .HasPrecision(18, 2);

        reading.Property(item => item.Source)
            .HasMaxLength(20)
            .HasDefaultValue("SIMULATED")
            .IsRequired();

        reading.HasIndex(item => new
        {
            item.SensorId,
            item.RecordedAt
        });
    }
}