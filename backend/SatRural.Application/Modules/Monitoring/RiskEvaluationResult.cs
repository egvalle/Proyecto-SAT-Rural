using SatRural.Domain.Common;

namespace SatRural.Application.Modules.Monitoring.Services;

public class RiskEvaluationResult
{
    public RiskLevel Level { get; set; }

    public string Message { get; set; } = string.Empty;
}