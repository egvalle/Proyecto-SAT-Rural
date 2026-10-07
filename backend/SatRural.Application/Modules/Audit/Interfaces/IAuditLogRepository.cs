using SatRural.Domain.Entities;

namespace SatRural.Application.Modules.Audit.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    Task<List<AuditLog>> GetAllAsync(
        CancellationToken cancellationToken = default);
}