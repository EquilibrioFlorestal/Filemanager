using Domain.Models;

namespace Domain.Interfaces
{
    public interface IBlocoService
    {
        Task<string> ListarBloco(uint idBloco, uint idCiclo);
    }
}
