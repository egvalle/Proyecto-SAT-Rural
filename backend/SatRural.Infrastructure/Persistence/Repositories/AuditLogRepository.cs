using Microsoft.EntityFrameworkCore;
using SatRural.Application.Modules.Audit.Interfaces;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public AuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Include(auditLog => auditLog.User)
            .OrderByDescending(auditLog => auditLog.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}