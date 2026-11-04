namespace SatRural.SensorService.Entities;

public class Sensor
{
    public int Id { get; set; }

    // Identificador externo de CommunityService; no es una FK local.
    public int CommunityId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTime? InstallationDate { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // SIMULATED o DEVICE. El servicio usará esto para no simular
    // lecturas en un sensor que ya recibe datos del ESP32.
    public string Source { get; set; } = "SIMULATED";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SensorReading> Readings { get; set; } =
        new List<SensorReading>();
}