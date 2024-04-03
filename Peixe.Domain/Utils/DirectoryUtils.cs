using Domain.Adapters;

namespace Domain.Utils;

public abstract class DirectoryUtils
{
    public static List<OrderFileProcessing> ProcurarArquivos(OrderProcessing requisicao, string extensao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        extensao = extensao.Replace(".", string.Empty);

        List<string> localArquivos = requisicao.PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{requisicao.Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).ToList();
        
        List<OrderFileProcessing> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessing(arquivo, requisicao.PastaDestino, requisicao.PastaBackup, requisicao.Modulo, requisicao.IdEmpresa)));

        return listaArquivos;
    }
}