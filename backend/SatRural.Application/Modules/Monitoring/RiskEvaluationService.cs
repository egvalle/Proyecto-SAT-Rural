using SatRural.Application.Modules.Monitoring.Interfaces;
using SatRural.Domain.Common;

namespace SatRural.Application.Modules.Monitoring.Services;

public class RiskEvaluationService
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public RiskEvaluationService(
        IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    public async Task<RiskEvaluationResult> EvaluateAsync(
        decimal temperature,
        decimal humidity,
        decimal windSpeed,
        decimal rainfall,
        decimal riverLevel,
        CancellationToken cancellationToken = default)
    {
        var rules = await _alertRuleRepository.GetAllAsync(
            cancellationToken);

        var values = new Dictionary<string, decimal>
        {
            ["TEMPERATURE"] = temperature,
            ["HUMIDITY"] = humidity,
            ["WIND_SPEED"] = windSpeed,
            ["RAINFALL"] = rainfall,
            ["RIVER_LEVEL"] = riverLevel
        };

        var results = new List<RiskEvaluationResult>();

        foreach (var rule in rules)
        {
            if (!values.TryGetValue(
                    rule.SensorType,
                    out var value))
            {
                continue;
            }

            if (!EvaluateCondition(
                    value,
                    rule.Operator,
                    rule.ThresholdValue))
            {
                continue;
            }

            if (!Enum.TryParse<RiskLevel>(
                    rule.RiskLevel,
                    true,
                    out var riskLevel))
            {
                continue;
            }

            results.Add(new RiskEvaluationResult
            {
                Level = riskLevel,
                Phenomenon = rule.Phenomenon,
                Message = rule.Description
            });
        }

        if (results.Count == 0)
        {
            return new RiskEvaluationResult
            {
                Level = RiskLevel.Green,
                Phenomenon = string.Empty,
                Message = "Todos los parámetros se encuentran dentro del rango normal."
            };
        }

        return results
            .OrderByDescending(result => result.Level)
            .First();
    }

    private static bool EvaluateCondition(
        decimal value,
        string operatorValue,
        decimal threshold)
    {
        return operatorValue switch
        {
            ">=" => value >= threshold,
            "<=" => value <= threshold,
            _ => false
        };
    }
}