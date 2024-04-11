using Domain.Adapters;

namespace Domain.Interfaces;

public interface IImagemService
{
    Task<bool> VerificarCadastrado(string nomeImagem, string programacaoRetornoGuid);
    Task<Tuple<bool, string>> CadastrarImagem(OrderImageProcessing requisicao);
}