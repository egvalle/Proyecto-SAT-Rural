using SatRural.Application.Modules.Audit.Interfaces;
using SatRural.Domain.Entities;

namespace SatRural.Application.Modules.Audit.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(
        int userId,
        string action,
        string entityName,
        string entityId,
        string details,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(
            auditLog,
            cancellationToken);
    }

    public async Task<List<AuditLogDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await _auditLogRepository.GetAllAsync(
            cancellationToken);

        return auditLogs.Select(auditLog => new AuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Username = auditLog.User.Username,
            FullName = auditLog.User.FullName,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            Details = auditLog.Details,
            CreatedAt = auditLog.CreatedAt
        }).ToList();
    }
}