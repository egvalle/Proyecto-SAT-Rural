using Microsoft.EntityFrameworkCore;
using SatRural.Application.Modules.Monitoring.Interfaces;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Repositories;

public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly AppDbContext _dbContext;

    public AlertRuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AlertRule>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AlertRules
            .AsNoTracking()
            .OrderBy(rule => rule.SensorType)
            .ThenBy(rule => rule.ThresholdValue)
            .ToListAsync(cancellationToken);
    }

    public async Task<AlertRule?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AlertRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rule => rule.Id == id,
                cancellationToken);
    }

    public async Task<AlertRule> CreateAsync(
        AlertRule alertRule,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AlertRules.Add(alertRule);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return alertRule;
    }

    public async Task<bool> UpdateAsync(
        AlertRule alertRule,
        CancellationToken cancellationToken = default)
    {
        var existingRule = await _dbContext.AlertRules
            .FirstOrDefaultAsync(
                rule => rule.Id == alertRule.Id,
                cancellationToken);

        if (existingRule is null)
        {
            return false;
        }

        existingRule.SensorType = alertRule.SensorType;
        existingRule.Operator = alertRule.Operator;
        existingRule.ThresholdValue = alertRule.ThresholdValue;
        existingRule.RiskLevel = alertRule.RiskLevel;
        existingRule.Phenomenon = alertRule.Phenomenon;
        existingRule.Description = alertRule.Description;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var existingRule = await _dbContext.AlertRules
            .FirstOrDefaultAsync(
                rule => rule.Id == id,
                cancellationToken);

        if (existingRule is null)
        {
            return false;
        }

        _dbContext.AlertRules.Remove(existingRule);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}