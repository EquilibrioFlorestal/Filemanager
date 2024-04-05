using Domain.Adapters;
using Microsoft.Extensions.Configuration;

namespace Domain.Utils;

public static class DirectoryUtils
{
    private static ushort _maxBatchFiles = 20;
    
    public static List<OrderFileProcessing> ProcurarArquivos(OrderProcessing requisicao, string extensao, CancellationToken cancellationToken, ushort? maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (maxBatch.HasValue) _maxBatchFiles = maxBatch.Value;
        
        extensao = extensao.Replace(".", string.Empty);

        List<string> localArquivos = requisicao.PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{requisicao.Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(_maxBatchFiles).ToList();
        
        List<OrderFileProcessing> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessing(arquivo, requisicao.PastaDestino, requisicao.PastaBackup, requisicao.Modulo, requisicao.IdEmpresa)));

        return listaArquivos;
    }
}