namespace Domain.Models;

public class Arquivo
{
    public int Id { get; set; }
    
    public uint IdEmpresa { get; set; }
    public required string NomeUsuario { get; set; }
    public required string NomeMaquina { get; set; }
    public required string NomeArquivo { get; set; }
    public long TamanhoBytes { get; set; }
    public ushort QuantidadeImagens { get; set; }
    public required string Modulo { get; set; }
    public ushort QuantidadeTalhoes { get; set; }
    public DateTime CreateAt { get; set; }

}