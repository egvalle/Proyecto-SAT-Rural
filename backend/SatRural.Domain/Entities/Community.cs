namespace SatRural.Domain.Entities;

public class Community
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}