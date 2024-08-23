using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peixe.Database.Context;

namespace Peixe.Database.Services;

public class BlocoService : IBlocoService
{
    private readonly IServiceProvider _serviceProvider;

    public BlocoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<String> ListarBloco(UInt32 idBloco, UInt32 IdCiclo)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using EPFDbContext context = scope.ServiceProvider.GetRequiredService<EPFDbContext>();

        try
        {
            return await context.Blocos
                .Where(x => x.Id == idBloco && x.IdCiclo == IdCiclo)
                .Select(x => x.Descricao)
                .FirstOrDefaultAsync() ?? String.Empty;
        }
        catch (Exception)
        {
            return String.Empty;
        }
    }
}
