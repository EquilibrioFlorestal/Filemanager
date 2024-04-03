namespace Domain.Models;

public class Talhao
{
    public int Id { get; set; }
    public int IdArea { get; set; }
    public int IdBloco { get; set; }
    public int IdSituacao { get; set; }
    public DateTime DataSituacao { get; set; }
    public int IdMotivo { get; set; }
    public string? Motivo { get; set; }
    public int IdUsuario { get; set; }
    public int IdEquipe { get; set; }
    public string ImeiColetor { get; set; }
    public string? Observacao { get; set; }
    public string ProgramacaoGuid { get; set; }
    public string ProgramacaoRetornoGuid { get; set; }
    public int IdExportacao { get; set; }
    public char SnNovo { get; set; }
    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public string NomeArquivo { get; set; }
    public string IdCiclo { get; set; }
    public int IdEmpresa { get; set; }
    public string Modulo { get; set; }
    public DateTime CreateAt { get; set; }
}