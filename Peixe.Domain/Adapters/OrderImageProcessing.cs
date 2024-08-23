namespace Domain.Adapters;

public class OrderImageProcessing
{
    public Guid Guid { get; set; }
    public String ProgramacaoRetornoGuid { get; set; }
    public String NomeImagem { get; set; }
    public String CaminhoArquivoZip { get; set; }

    public OrderImageProcessing(String programacaoRetornoGuid, String nomeImagem, String caminhoArquivoZip)
    {
        Guid = Guid.NewGuid();

        ProgramacaoRetornoGuid = programacaoRetornoGuid;
        NomeImagem = Path.GetFileName(nomeImagem);
        CaminhoArquivoZip = caminhoArquivoZip;
    }
}