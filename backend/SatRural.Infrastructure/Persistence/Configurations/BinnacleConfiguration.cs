using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class BinnacleConfiguration : IEntityTypeConfiguration<Binnacle>
{
    public void Configure(EntityTypeBuilder<Binnacle> builder)
    {
        builder.Property(binnacle => binnacle.Description)
            .IsRequired();

        builder.Property(binnacle => binnacle.User)
            .IsRequired();

        builder.Property(binnacle => binnacle.DateHour)
            .IsRequired();

    }
}
