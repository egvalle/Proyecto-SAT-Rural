using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.Property(x => x.DetectedValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.ThresholdValue)
            .HasPrecision(18, 2);
    }
}