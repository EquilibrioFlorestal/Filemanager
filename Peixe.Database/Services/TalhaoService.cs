using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peixe.Database.Context;
using Serilog;

namespace Peixe.Database.Services;

public class TalhaoService(AppDbContext context) : ITalhaoService
{
    private readonly AppDbContext _context = context;

    public Task<bool> VerificarCadastrado(string nomeArquivo, string programacaoRetornoGuid)
    {
        return Task.FromResult(_context.Talhoes.Any(x => x.ProgramacaoRetornoGuid == programacaoRetornoGuid));
    }

    public async Task<Tuple<bool, string>> CadastrarTalhao(OrderTalhaoProcessing requisicao)
    {
        _context.Talhoes.Add(new Talhao
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
            IdUsuario = Convert.ToInt32(requisicao.IdSituacao),
            ImeiColetor = requisicao.ImeiColetor,
            ProgramacaoGuid = requisicao.ProgramacaoGuid,
            SnNovo = requisicao.SnNovo ?? 'N',
        });
        try
        {
            await _context.SaveChangesAsync();
            return await Task.FromResult(Tuple.Create(true, string.Empty));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Tuple.Create(false, ex.Message));
        }
    }
}