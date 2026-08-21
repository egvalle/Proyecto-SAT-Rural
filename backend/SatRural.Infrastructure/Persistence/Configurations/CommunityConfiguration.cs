using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        builder.HasData(
            new Community
            {
                Id = 1,
                Name = "Comunidad El Pinar",
                Latitude = 14.634915m,
                Longitude = -90.506882m,
                IsActive = true,
                CreatedAt = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}