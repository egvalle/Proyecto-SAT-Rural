using SatRural.Domain.Common;

namespace SatRural.Application.Modules.Monitoring.Services;

public class RiskEvaluationResult
{
    public RiskLevel Level { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Phenomenon { get; set; } = string.Empty;

    public int? AlertRuleId { get; set; }

    public string? SensorType { get; set; }

    public string? Operator { get; set; }

    public decimal? DetectedValue { get; set; }

    public decimal? ThresholdValue { get; set; }
}