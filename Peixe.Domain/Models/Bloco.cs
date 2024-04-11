namespace Domain.Models
{
    public class Bloco
    {
        public int Id { get; set; }
        public int IdCiclo { get; set; }
        public required string Descricao { get; set; }
    }
}
