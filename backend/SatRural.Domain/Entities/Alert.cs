namespace SatRural.Domain.Entities;

public class Alert
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Phenomenon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}