using Domain.Adapters;

namespace Domain.Interfaces;

public interface ITalhaoService
{
    Task<bool> VerificarCadastrado(string nomeArquivo, string programacaoRetornoGuid);
    Task<bool> CadastrarTalhao(OrderTalhaoProcessing requisicao);
}