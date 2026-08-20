namespace SatRural.Domain.Entities;

public class Sensor
{
    public int Id { get; set; }

    public int CommunityId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Community Community { get; set; } = null!;

    public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}