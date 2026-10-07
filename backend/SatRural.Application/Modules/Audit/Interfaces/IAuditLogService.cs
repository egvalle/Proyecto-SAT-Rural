using SatRural.Application.Modules.Audit;

namespace SatRural.Application.Modules.Audit.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        int userId,
        string action,
        string entityName,
        string entityId,
        string details,
        CancellationToken cancellationToken = default);

    Task<List<AuditLogDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}