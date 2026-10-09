using SatRural.Domain.Entities;

namespace SatRural.Application.Modules.Monitoring.Interfaces;

public interface IAlertRuleRepository
{
    Task<List<AlertRule>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<AlertRule?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AlertRule> CreateAsync(
        AlertRule alertRule,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        AlertRule alertRule,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}