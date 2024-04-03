using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Peixe.Database.Context;
using Serilog;

namespace Peixe.Database.Services;

public class ArquivoService(IMediator mediator, AppDbContext context) : IArquivoService
{
    private readonly AppDbContext _context = context;
    private readonly IMediator _mediator = mediator;
    
    public Task<bool> VerificarCadastrado(string nomeArquivo, string modulo, int idEmpresa)
    {
        return Task.FromResult(_context.Arquivos
            .Any(x => x.NomeArquivo == nomeArquivo));
    }

    public async Task<bool> CadastrarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
    {
        try
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

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            await _mediator.Publish(new ErroAdicionarArquivoNotification(requisicaoArquivo, e.Message));
            return false;
        }
        
    }
}