using Domain.Adapters;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Peixe.Database.Context;

namespace Peixe.Database.Services;

public class ArquivoService : IArquivoService
{
    private readonly IServiceProvider _serviceProvider;

    public ArquivoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Boolean> VerificarCadastrado(String nomeArquivo, String modulo, Int32 idEmpresa)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return Task.FromResult(context.Arquivos.Select(x => x.NomeArquivo).Any(x => x == nomeArquivo));
    }

    public async Task<Tuple<Boolean, String>> CadastrarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Arquivos.Add(new Arquivo
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
            await context.SaveChangesAsync();
            return Tuple.Create(true, String.Empty);
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, ex.Message);
        }

    }
}