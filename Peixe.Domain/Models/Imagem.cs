namespace Domain.Models;

public class Imagem
{
    public Int32 Id { get; set; } // _ID
    public required String CaminhoArquivoZip { get; set; } // caminho_zip
    public required String NomeImagem { get; set; } // nome_imagem
    public required String ProgramacaoRetornoGuid { get; set; } // id_programacao_retorno_guid
    public DateTime CreateAt { get; set; } // create_at
}