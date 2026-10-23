namespace SatRural.Domain.Entities;

public class Attachment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Urlcomplete { get; set; } = string.Empty;
    public string Urlrelative { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public int? IdClase { get; set; }
    public Clase? Clase { get; set; }
}