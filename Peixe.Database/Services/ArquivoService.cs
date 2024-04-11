using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Peixe.Database.Context;
using Serilog;

namespace Peixe.Database.Services;

public class ArquivoService(AppDbContext context) : IArquivoService
{
    private readonly AppDbContext _context = context;

    public Task<bool> VerificarCadastrado(string nomeArquivo, string modulo, int idEmpresa)
    {
        return Task.FromResult(_context.Arquivos.Select(x => x.NomeArquivo).Any(x => x == nomeArquivo));
    }

    public async Task<Tuple<bool, string>> CadastrarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
    {
        _context.Arquivos.Add(new Arquivo
        {
            IdEmpresa = requisicao.IdEmpresa,
            Modulo = requisicao.Modulo,
            NomeArquivo = requisicaoArquivo.Nome,
            NomeMaquina = requisicao.NomeMaquina,
            NomeUsuario = requisicao.NomeUsuario,
            QuantidadeImagens = requisicaoArquivo.QuantidadeImagens,
            QuantidadeTalhoes = requisicaoArquivo.QuantidadeTalhoes,
            TamanhoBytes = requisicaoArquivo.TamanhoBytes,
            CreateAt = DateTime.Now
        });

        try
        {
            await _context.SaveChangesAsync();
            return Tuple.Create(true, string.Empty);
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, ex.Message);
        }

    }
}