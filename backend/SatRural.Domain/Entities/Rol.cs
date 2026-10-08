namespace SatRural.Domain.Entities;

public class Rol
{
    public const int AdminId = 1;
    public const int UserId = 2;
    public const int UserConsultaId = 3;

    public int Id { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
