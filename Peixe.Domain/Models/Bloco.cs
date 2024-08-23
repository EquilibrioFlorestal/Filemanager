namespace Domain.Models;

public class Bloco
{
    public Int32 Id { get; set; }
    public Int32 IdCiclo { get; set; }
    public required String Descricao { get; set; }
}
