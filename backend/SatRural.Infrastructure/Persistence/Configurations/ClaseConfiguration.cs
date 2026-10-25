using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class ClaseConfiguration : IEntityTypeConfiguration<Clase>
{
    public void Configure(EntityTypeBuilder<Clase> builder)
    {
        builder.Property(clase => clase.Nombre)
            .HasColumnName("Clase")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(clase => clase.Descripcion)
            .IsRequired()
            .HasMaxLength(1000);
    }
}
