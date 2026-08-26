using Microsoft.EntityFrameworkCore;

using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Community> Communities =>
        Set<Community>();

    public DbSet<Sensor> Sensors =>
        Set<Sensor>();

    public DbSet<SensorReading> SensorReadings =>
        Set<SensorReading>();

    public DbSet<Alert> Alerts =>
        Set<Alert>();

    public DbSet<AlertRule> AlertRules =>
        Set<AlertRule>();

    public DbSet<Event> Events =>
        Set<Event>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly
        );
    }
}