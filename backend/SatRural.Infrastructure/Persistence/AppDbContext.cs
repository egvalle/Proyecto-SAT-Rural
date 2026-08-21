using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
}