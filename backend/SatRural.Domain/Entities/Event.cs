namespace SatRural.Domain.Entities;

public class Event
{
    public long Id { get; set; }

    public long? AlertId { get; set; }

    public int CommunityId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Alert? Alert { get; set; }

    public Community Community { get; set; } = null!;
}