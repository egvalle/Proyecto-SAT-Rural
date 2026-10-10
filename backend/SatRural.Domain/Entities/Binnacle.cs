namespace SatRural.Domain.Entities;

public class Binnacle
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public int IdMovimentType { get; set; }

    public string User { get; set; } = string.Empty;

    public DateTime DateHour { get; set; }
}