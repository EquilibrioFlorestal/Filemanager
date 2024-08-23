using Domain.Adapters;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peixe.Database.Context;

namespace Peixe.Database.Services;
public class ProgramacaoService : IProgramacaoService
{
    private readonly IServiceProvider _serviceProvider;

    public ProgramacaoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Tuple<Programacao?, String>> Atualizar(OrderTalhaoProcessing request)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            using EPFDbContext context = scope.ServiceProvider.GetRequiredService<EPFDbContext>();

            Guid idProgramacaoGuidTalhao = Guid.TryParse(request.ProgramacaoGuid, out Guid guid) ? guid : Guid.Empty;

            Programacao? programacao = await context.Programacoes
                .Where(x => x.IdProgramacaoGuid == idProgramacaoGuidTalhao)
                .Where(x => x.IdSituacao == 1)
                .FirstOrDefaultAsync();

            if (programacao == null) return new(null, "");

            programacao = programacao.Atualizar(request);

            context.Update(programacao);
            await context.SaveChangesAsync();

            return new(programacao, "Sucesso.");

        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }
}
