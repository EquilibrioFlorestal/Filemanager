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
                .FirstOrDefaultAsync();

            if (programacao == null && request.SnNovo == 'S')
            {
                programacao = await Cadastrar(request);
                return new(programacao, "Sucesso.");
            }
            
            if (programacao == null)
                return new(null, "");            

            if (programacao.IdSituacao == 1)
            {
                programacao = programacao.Atualizar(request);

                context.Update(programacao);
                await context.SaveChangesAsync();

                return new(programacao, "Sucesso.");
            }

            return new(programacao, "");

        }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    public async Task<Programacao?> Cadastrar(OrderTalhaoProcessing request)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            using EPFDbContext context = scope.ServiceProvider.GetRequiredService<EPFDbContext>();

            Boolean parseGuid = Guid.TryParse(request.ProgramacaoGuid, out Guid idProgramacaoGuid);
            Boolean parseGuidRetorno = Guid.TryParse(request.ProgramacaoRetornoGuid, out Guid idProgramacaoRetornoGuid);

            Cadastro? cadastro = await context.Cadastros.Where(x => x.Id == request.IdArea).FirstOrDefaultAsync();

            cadastro ??= new Cadastro
            {
                Id = 1,
                Equipe = String.Empty
            };

            if (!parseGuid) await Task.CompletedTask;

            Programacao programacao = new()
            {
                IdAreaEmp = (Int32)request.IdArea,
                IdBloco = (Int32)request.IdBloco,
                IdTipoLevantamento = (Int32)request.IdTipoLevantamento,
                IdProgramacaoGuid = idProgramacaoGuid,
                IdSituacao = (Int32)request.IdSituacao,
                DataSituacao = request.DataSituacao,
                DataProgramacao = new DateTime(request.DataSituacao.Year, request.DataSituacao.Month, request.DataSituacao.Day, request.DataSituacao.AddHours(-1).Hour, 0, 0),
                IdMotivoSituacao = (Int32)request.IdMotivo,
                ObservacaoUsuario = request.Observacao,
                IdUsuarioSituacao = (Int32)request.IdUsuario,
                SnNovo = 'S',
                IdExportacao = (Int32)request.IdExportacao,
                Equipe = cadastro!.Equipe!.ToUpper(),
                IdEquipeSituacao = (Int32)request.IdEquipe,
                ImeiSituacao = request.ImeiColetor,
                IdProgramacaoRetornoGuid = idProgramacaoRetornoGuid,
                Longitude = Decimal.TryParse(request.Longitude.PadRight(12, '0').AsSpan(0, 12), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Decimal lng) ? lng : 0,
                Latitude = Decimal.TryParse(request.Latitude.PadRight(12, '0').AsSpan(0, 12), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out Decimal lat) ? lat : 0,
            };

            context.Programacoes.Add(programacao);
            await context.SaveChangesAsync(CancellationToken.None);

            return programacao;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
