namespace SatRural.Domain.Entities;

public class Community
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Municipality { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Country { get; set; } = "Guatemala";

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public ICollection<Event> Events { get; set; } = new List<Event>();
}