namespace SatRural.Application.Modules.Alerts;

public class AlertRuleDto
{
    public int Id { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Phenomenon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}