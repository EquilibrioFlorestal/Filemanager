using Domain.Adapters;

namespace Domain.Interfaces;

public interface IArquivoService
{
    Task<Boolean> VerificarCadastrado(String nomeArquivo, String modulo, Int32 idEmpresa);
    Task<Tuple<Boolean, String>> CadastrarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo);
}