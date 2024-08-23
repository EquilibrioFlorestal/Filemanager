namespace Domain.Models;

public class Talhao
{
    public Int32 Id { get; set; }
    public Int32 IdArea { get; set; }
    public Int32 IdBloco { get; set; }
    public Int32 IdSituacao { get; set; }
    public DateTime DataSituacao { get; set; }
    public Int32 IdMotivo { get; set; }
    public String? Motivo { get; set; }
    public Int32 IdUsuario { get; set; }
    public Int32 IdEquipe { get; set; }
    public String ImeiColetor { get; set; }
    public String? Observacao { get; set; }
    public String ProgramacaoGuid { get; set; }
    public String ProgramacaoRetornoGuid { get; set; }
    public Int32 IdExportacao { get; set; }
    public Char SnNovo { get; set; }
    public String Latitude { get; set; }
    public String Longitude { get; set; }
    public String NomeArquivo { get; set; }
    public String IdCiclo { get; set; }
    public Int32 IdEmpresa { get; set; }
    public String Modulo { get; set; }
    public DateTime CreateAt { get; set; }
}