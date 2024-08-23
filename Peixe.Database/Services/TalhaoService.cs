using Domain.Adapters;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Peixe.Database.Context;

namespace Peixe.Database.Services;

public class TalhaoService : ITalhaoService
{

    private readonly IServiceProvider _serviceProvider;

    public TalhaoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Boolean> VerificarCadastrado(String nomeArquivo, String programacaoRetornoGuid)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return Task.FromResult(context.Talhoes.Any(x => x.ProgramacaoRetornoGuid == programacaoRetornoGuid));
    }

    public async Task<Tuple<Boolean, String>> CadastrarTalhao(OrderTalhaoProcessing requisicao)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Talhoes.Add(new Talhao
        {
            IdEmpresa = Convert.ToInt32(requisicao.IdEmpresa),
            Modulo = requisicao.Modulo,
            NomeArquivo = requisicao.NomeArquivo,
            ProgramacaoRetornoGuid = requisicao.ProgramacaoRetornoGuid,
            CreateAt = DateTime.Now,
            Latitude = requisicao.Latitude,
            Longitude = requisicao.Longitude,
            Motivo = requisicao.Motivo,
            Observacao = requisicao.Observacao,
            DataSituacao = requisicao.DataSituacao,
            IdArea = Convert.ToInt32(requisicao.IdArea),
            IdBloco = Convert.ToInt32(requisicao.IdBloco),
            IdCiclo = requisicao.IdCiclo,
            IdEquipe = Convert.ToInt32(requisicao.IdEquipe),
            IdExportacao = Convert.ToInt32(requisicao.IdExportacao),
            IdMotivo = Convert.ToInt32(requisicao.IdMotivo),
            IdSituacao = Convert.ToInt32(requisicao.IdSituacao),
            IdUsuario = Convert.ToInt32(requisicao.IdUsuario),
            ImeiColetor = requisicao.ImeiColetor,
            ProgramacaoGuid = requisicao.ProgramacaoGuid,
            SnNovo = requisicao.SnNovo ?? 'N',
        });
        try
        {
            await context.SaveChangesAsync();
            return await Task.FromResult(Tuple.Create(true, String.Empty));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Tuple.Create(false, ex.Message));
        }
    }
}