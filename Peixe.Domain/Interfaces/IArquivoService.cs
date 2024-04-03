using Domain.Adapters;

namespace Domain.Interfaces;

public interface IArquivoService
{
    Task<bool> VerificarCadastrado(string nomeArquivo, string modulo, int idEmpresa);
    Task<bool> CadastrarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo);
}