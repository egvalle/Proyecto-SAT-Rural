using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Rol> Roles => Set<Rol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var roles = modelBuilder.Entity<Rol>();
        roles.ToTable("Roles");
        roles.HasKey(role => role.Id);
        roles.Property(role => role.Descripcion)
            .HasMaxLength(50)
            .IsRequired();
        roles.HasIndex(role => role.Descripcion).IsUnique();
        roles.HasData(
            new Rol { Id = Rol.AdminId, Descripcion = "ADMIN" },
            new Rol { Id = Rol.UserId, Descripcion = "USER" },
            new Rol { Id = Rol.UserConsultaId, Descripcion = "USERCONSULTA" });
        roles.HasMany(role => role.Users)
            .WithOne()
            .HasForeignKey(user => user.RolId);

        var users = modelBuilder.Entity<User>();
        users.ToTable("Users");
        users.HasKey(user => user.Id);
        users.Property(user => user.Username)
            .HasMaxLength(256)
            .IsRequired();
        users.HasIndex(user => user.Username).IsUnique();
        users.Property(user => user.PasswordHash).IsRequired();
        users.Property(user => user.FullName).IsRequired();
        users.Ignore(user => user.AuditLogs);
    }
}
