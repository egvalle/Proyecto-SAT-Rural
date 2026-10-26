namespace SatRural.Domain.Entities;

public class Clase
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}