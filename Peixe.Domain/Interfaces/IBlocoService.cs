namespace Domain.Interfaces;

public interface IBlocoService
{
    Task<String> ListarBloco(UInt32 idBloco, UInt32 idCiclo);
}
