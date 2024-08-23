namespace Domain.Models;

public class Arquivo
{
    public Int32 Id { get; set; }

    public UInt32 IdEmpresa { get; set; }
    public required String NomeUsuario { get; set; }
    public required String NomeMaquina { get; set; }
    public required String NomeArquivo { get; set; }
    public Int64 TamanhoBytes { get; set; }
    public UInt16 QuantidadeImagens { get; set; }
    public required String Modulo { get; set; }
    public UInt16 QuantidadeTalhoes { get; set; }
    public DateTime CreateAt { get; set; }

}