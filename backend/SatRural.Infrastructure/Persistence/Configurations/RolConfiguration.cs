using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.HasKey(rol => rol.Id);

        builder.Property(rol => rol.Descripcion)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(rol => rol.Descripcion)
            .IsUnique();

        builder.HasData(
            new Rol { Id = Rol.AdminId, Descripcion = "ADMIN" },
            new Rol { Id = Rol.UserId, Descripcion = "USER" },
            new Rol { Id = Rol.UserConsultaId, Descripcion = "USERCONSULTA" });
    }
}
