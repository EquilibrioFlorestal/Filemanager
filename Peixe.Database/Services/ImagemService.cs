using Domain.Adapters;
using Domain.Interfaces;
using Domain.Models;
using Peixe.Database.Context;

namespace Peixe.Database.Services;

public class ImagemService(AppDbContext context) : IImagemService
{
    private readonly AppDbContext _context = context;
    
    public Task<bool> VerificarCadastrado(string nomeImagem, string programacaoRetornoGuid)
    {
        return Task.FromResult(_context.Imagens
            .Any(x => x.NomeImagem == nomeImagem && x.ProgramacaoRetornoGuid == programacaoRetornoGuid));
    }

    public async Task<Tuple<bool, string>> CadastrarImagem(OrderImageProcessing requisicao)
    {
        try
        {
            _context.Imagens.Add(new Imagem
            {
                NomeImagem = requisicao.NomeImagem,
                CaminhoArquivoZip = requisicao.CaminhoArquivoZip,
                ProgramacaoRetornoGuid = requisicao.ProgramacaoRetornoGuid,
                CreateAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Tuple.Create(true, string.Empty);
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, ex.Message);
        }
    }
}