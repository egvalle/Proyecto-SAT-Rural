namespace SatRural.SensorService.Entities;

public class SensorReading
{
    public long Id { get; set; }

    public int SensorId { get; set; }

    public decimal Value { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Copia el origen de la medición para conservarlo en el historial.
    public string Source { get; set; } = "SIMULATED";

    public Sensor Sensor { get; set; } = null!;
}