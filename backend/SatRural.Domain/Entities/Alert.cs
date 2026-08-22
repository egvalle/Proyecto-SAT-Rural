namespace SatRural.Domain.Entities;

public class Alert
{
    public long Id { get; set; }

    public int CommunityId { get; set; }

    public int? SensorId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public Community Community { get; set; } = null!;

    public Sensor? Sensor { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}