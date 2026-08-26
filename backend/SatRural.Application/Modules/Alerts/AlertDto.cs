namespace SatRural.Application.Modules.Alerts;

public class AlertDto
{
    public long Id { get; set; }

    public int CommunityId { get; set; }

    public int? SensorId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public bool IsActive { get; set; }
}