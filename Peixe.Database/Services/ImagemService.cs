using Domain.Adapters;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Peixe.Database.Context;

namespace Peixe.Database.Services;

public class ImagemService : IImagemService
{
    private readonly IServiceProvider _serviceProvider;

    public ImagemService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<Boolean> VerificarCadastrado(String nomeImagem, String programacaoRetornoGuid)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return Task.FromResult(context.Imagens
            .Any(x => x.NomeImagem == nomeImagem && x.ProgramacaoRetornoGuid == programacaoRetornoGuid));
    }

    public async Task<Tuple<Boolean, String>> CadastrarImagem(OrderImageProcessing requisicao)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            context.Imagens.Add(new Imagem
            {
                NomeImagem = requisicao.NomeImagem,
                CaminhoArquivoZip = requisicao.CaminhoArquivoZip,
                ProgramacaoRetornoGuid = requisicao.ProgramacaoRetornoGuid,
                CreateAt = DateTime.Now
            });

            await context.SaveChangesAsync();
            return Tuple.Create(true, String.Empty);
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, ex.Message);
        }
    }
}