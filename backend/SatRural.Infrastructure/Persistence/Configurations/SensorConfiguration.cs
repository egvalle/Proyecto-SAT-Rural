using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(x => x.Community)
            .WithMany(x => x.Sensors)
            .HasForeignKey(x => x.CommunityId)
            .OnDelete(DeleteBehavior.Restrict);

        var createdAt =
            new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Sensor
            {
                Id = 1,
                CommunityId = 1,
                Code = "TEMP-001",
                Name = "Temperatura Ambiente",
                Type = "TEMPERATURE",
                Unit = "°C",
                IsActive = true,
                CreatedAt = createdAt
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
                CreatedAt = createdAt
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
                CreatedAt = createdAt
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
                CreatedAt = createdAt
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
                CreatedAt = createdAt
            }
        );
    }
}