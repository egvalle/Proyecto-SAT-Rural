namespace SatRural.Application.Modules.Events;

public class EventDto
{
    public long Id { get; set; }

    public long? AlertId { get; set; }

    public int CommunityId { get; set; }

    public string CommunityName { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public int? SensorId { get; set; }

    public string? Level { get; set; }

    public bool? IsActive { get; set; }
}