using Domain.Adapters;
using Domain.Models;

namespace Domain.Interfaces;
public interface IProgramacaoService
{
    Task<Tuple<Programacao?, String>> Atualizar(OrderTalhaoProcessing request);
}
