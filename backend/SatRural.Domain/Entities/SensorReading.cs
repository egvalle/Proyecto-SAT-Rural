namespace SatRural.Domain.Entities;

public class SensorReading
{
    public long Id { get; set; }

    public int SensorId { get; set; }

    public decimal Value { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Sensor Sensor { get; set; } = null!;
}