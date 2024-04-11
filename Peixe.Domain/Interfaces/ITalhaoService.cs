using Domain.Adapters;

namespace Domain.Interfaces;

public interface ITalhaoService
{
    Task<bool> VerificarCadastrado(string nomeArquivo, string programacaoRetornoGuid);
    Task<Tuple<bool, string>> CadastrarTalhao(OrderTalhaoProcessing requisicao);
}