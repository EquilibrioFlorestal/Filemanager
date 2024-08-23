using Domain.Adapters;

namespace Domain.Interfaces;

public interface ITalhaoService
{
    Task<Boolean> VerificarCadastrado(String nomeArquivo, String programacaoRetornoGuid);
    Task<Tuple<Boolean, String>> CadastrarTalhao(OrderTalhaoProcessing requisicao);
}