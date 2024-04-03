namespace Domain.Models;

public class Imagem
{
    public int Id { get; set; } // _ID
    public required string CaminhoArquivoZip { get; set; } // caminho_zip
    public required string NomeImagem { get; set; } // nome_imagem
    public required string ProgramacaoRetornoGuid { get; set; } // id_programacao_retorno_guid
    public DateTime CreateAt { get; set; } // create_at
}