namespace Domain.Adapters;

public class OrderImageProcessing
{
    public Guid Guid { get; set; }
    public string ProgramacaoRetornoGuid { get; set; }
    public string NomeImagem { get; set; }
    public string CaminhoArquivoZip { get; set; }
    
    public OrderImageProcessing(string programacaoRetornoGuid, string nomeImagem, string caminhoArquivoZip)
    {
        Guid = Guid.NewGuid();
        
        ProgramacaoRetornoGuid = programacaoRetornoGuid;
        NomeImagem = Path.GetFileName(nomeImagem);
        CaminhoArquivoZip = caminhoArquivoZip;
    }
}