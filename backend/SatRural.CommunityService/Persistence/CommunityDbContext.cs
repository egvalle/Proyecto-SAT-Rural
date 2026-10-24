using Microsoft.EntityFrameworkCore;
using SatRural.CommunityService.Models;

namespace SatRural.CommunityService.Persistence;

public sealed class CommunityDbContext : DbContext
{
    public CommunityDbContext(
        DbContextOptions<CommunityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Community> Communities => Set<Community>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var community = modelBuilder.Entity<Community>();

        community.ToTable("Communities");
        community.HasKey(x => x.Id);

        community.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        community.Property(x => x.Municipality)
            .HasMaxLength(100)
            .IsRequired();

        community.Property(x => x.Department)
            .HasMaxLength(100)
            .IsRequired();

        community.Property(x => x.Country)
            .HasMaxLength(100)
            .IsRequired();

        community.Property(x => x.Latitude)
            .HasPrecision(9, 6);

        community.Property(x => x.Longitude)
            .HasPrecision(9, 6);

        community.Property(x => x.Description)
            .HasMaxLength(500);

        community.HasData(new Community
        {
            Id = 1,
            Name = "Comunidad El Pinar",
            Municipality = "Guatemala",
            Department = "Guatemala",
            Country = "Guatemala",
            Latitude = 14.634915m,
            Longitude = -90.506882m,
            Description = "Comunidad inicial del sistema de monitoreo.",
            IsActive = true,
            CreatedAt = new DateTime(
                2026, 8, 20, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}