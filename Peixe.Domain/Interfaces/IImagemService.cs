using Domain.Adapters;

namespace Domain.Interfaces;

public interface IImagemService
{
    Task<Boolean> VerificarCadastrado(String nomeImagem, String programacaoRetornoGuid);
    Task<Tuple<Boolean, String>> CadastrarImagem(OrderImageProcessing requisicao);
}