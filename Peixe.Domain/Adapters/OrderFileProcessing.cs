using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Domain.Utils;
using ICSharpCode.SharpZipLib.Zip;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace Domain.Adapters;

public class OrderFileProcessing
{
    public Guid Guid { get; init; }
    public string Nome { get; set; }
    public string NomeSemExtensao { get; set; }
    public string Extensao { get; set; }
    public long TamanhoBytes { get; init; }

    protected uint IdEmpresa { get; set; }
    protected string Modulo { get; set; }
    protected uint IdExportacao { get; set; }
    public uint IdCiclo { get; set; }
    
    public ushort QuantidadeImagens { get; set; }
    public ushort QuantidadeTalhoes { get; set; }
    
    public string CaminhoOrigem { get; set; }
    public string CaminhoDestino { get; set; }
    public string CaminhoBackup { get; set; }

    public string DiretorioDestino { get; set; }
    public string DiretorioBackup { get; set; }

    protected string? HashOrigem { get; set; }
    protected string? HashDestino { get; set; }
    protected string? HashBackup { get; set; }

    protected bool ArquivoZipValido { get; set; }
    protected bool ProcessamentoValido { get; set; }

    public DateTime DataProcessamento { get; init; }

    public List<OrderImageProcessing> OrderImagens { get; set; }
    public List<OrderTalhaoProcessing> OrderTalhoes { get; set; }


    public OrderFileProcessing(string caminhoOrigem, string diretorioDestino, string diretorioBackup, string modulo, uint idEmpresa)
    {
        FileInfo fileInfo = new FileInfo(caminhoOrigem);

        Guid = Guid.NewGuid();
        Nome = Path.GetFileName(caminhoOrigem);
        NomeSemExtensao = Path.GetFileNameWithoutExtension(caminhoOrigem);
        Extensao = Path.GetExtension(caminhoOrigem);
        TamanhoBytes = fileInfo.Length;
        Modulo = modulo;
        IdEmpresa = idEmpresa;
        
        Regex regexIdCiclo = new Regex(@$"{Modulo}_(\d+)_.*");
        IdCiclo = Convert.ToUInt16(regexIdCiclo.Match(Nome).Groups[1].Value);
        
        CaminhoOrigem = caminhoOrigem;

        CaminhoDestino = Path.Combine(diretorioDestino, Nome);
        DiretorioDestino = diretorioDestino;

        CaminhoBackup = Path.Combine(diretorioBackup, Nome);
        DiretorioBackup = diretorioBackup;

        DataProcessamento = DateTime.Now;
        ProcessamentoValido = false;

        OrderImagens = new List<OrderImageProcessing>();
        OrderTalhoes = new List<OrderTalhaoProcessing>();
        
        DefinirHashOrigem();
    }

    protected void DefinirHashOrigem()
    {
        string hashFile = GerarHashArquivo(CaminhoOrigem);
        HashOrigem = hashFile;
    }

    protected void DefinirHashDestino()
    {
        string hashFile = GerarHashArquivo(CaminhoDestino);
        HashDestino = hashFile;
    }

    protected void DefinirHashBackup()
    {
        string hashFile = GerarHashArquivo(CaminhoBackup);
        HashBackup = hashFile;
    }

    public virtual bool ValidarArquivoZip()
    {
        byte[] zipBytes = File.ReadAllBytes(CaminhoOrigem);
        byte[] xmlBytes;

        try
        {
            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            {
                using (ZipFile zipFile = new ZipFile(zipStream))
                {
                    zipFile.Password = @"epf@1387.01#";

                    QuantidadeImagens = Convert.ToUInt16(zipFile.Cast<ZipEntry>().Count(zipEntry => zipEntry.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));

                    if (!zipFile.Cast<ZipEntry>().Any(zipEntry =>
                            zipEntry.Name.Equals("programacao.xml", StringComparison.OrdinalIgnoreCase)))
                    {
                        return ArquivoZipValido = false;
                    }

                    ZipEntry entry = zipFile.GetEntry("programacao.xml");

                    using (MemoryStream entryStream = new MemoryStream())
                    {
                        zipFile.GetInputStream(entry).CopyTo(entryStream);
                        xmlBytes = entryStream.ToArray();
                    }
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(System.Text.Encoding.UTF8.GetString(xmlBytes));

                XmlNode? exportacoes = xmlDoc.SelectSingleNode("//dataset/exportacao");
                
                IdExportacao = Convert.ToUInt16(exportacoes?.SelectSingleNode("_id")?.InnerText);

                XmlNodeList? nodes = xmlDoc.SelectNodes("//dataset/programacao_retorno");

                QuantidadeTalhoes = Convert.ToUInt16(nodes?.Count);

                foreach (XmlNode programacao in nodes)
                {
                    OrderTalhoes.Add(ProcessarTalhao(programacao));
                }
                
                XmlNodeList? nodesImagem = xmlDoc.SelectNodes("//dataset/programacao_imagem");

                if (nodesImagem == null) return ArquivoZipValido = true;
                
                foreach (XmlNode imagem in nodesImagem)
                {
                    OrderImagens.Add(ProcessarImagem(imagem));
                }

                return ArquivoZipValido = true;
            }
        }
        catch (ZipException)
        {
            return ArquivoZipValido = false;
        }
        catch (Exception)
        {
            return ArquivoZipValido = false;
        }
        
    }

    protected OrderTalhaoProcessing ProcessarTalhao(XmlNode talhao)
    {
        return new OrderTalhaoProcessing
        {
            DataSituacao = DateTime.ParseExact(talhao.SelectSingleNode("dt_situacao")?.InnerText ?? string.Empty,
                "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Id = Convert.ToUInt32(talhao.SelectSingleNode("_id")?.InnerText ?? string.Empty),
            IdArea = Convert.ToUInt32(talhao.SelectSingleNode("id_area_emp")?.InnerText ?? string.Empty),
            IdBloco = Convert.ToUInt32(talhao.SelectSingleNode("id_bloco")?.InnerText ?? string.Empty),

            Latitude = talhao.SelectSingleNode("vl_latitude")?.InnerText ?? string.Empty,
            Longitude = talhao.SelectSingleNode("vl_longitude")?.InnerText ?? string.Empty,

            Modulo = Modulo.ToString(),
            IdCiclo = IdCiclo.ToString(),
            ImeiColetor = talhao.SelectSingleNode("cd_imei_situacao")?.InnerText ?? string.Empty,
            NomeArquivo = Nome.ToString(),

            ProgramacaoGuid = talhao.SelectSingleNode("id_programacao_guid")?.InnerText ?? string.Empty,
            ProgramacaoRetornoGuid = talhao.SelectSingleNode("id_programacao_retorno_guid")?.InnerText ?? string.Empty,

            Motivo = talhao.SelectSingleNode("ds_motivo_obs")?.InnerText ?? string.Empty,
            IdEmpresa = IdEmpresa,
            Observacao = talhao.SelectSingleNode("ds_obs")?.InnerText ?? string.Empty,
            IdEquipe = Convert.ToUInt16(talhao.SelectSingleNode("id_equipe_situacao")?.InnerText),
            IdExportacao = IdExportacao,
            IdMotivo = Convert.ToUInt16(talhao.SelectSingleNode("id_motivo")?.InnerText),
            IdSituacao = Convert.ToUInt16(talhao.SelectSingleNode("id_situacao")?.InnerText),
            IdUsuario = Convert.ToUInt16(talhao.SelectSingleNode("id_usuario_situacao")?.InnerText),
            SnNovo = talhao.SelectSingleNode("sn_novo")?.InnerText.ToCharArray().FirstOrDefault(),

        };
    }

    protected OrderImageProcessing ProcessarImagem(XmlNode imagem)
    {
        string nomeImagem = imagem.SelectSingleNode("no_imagem")?.InnerText ?? string.Empty;
        string programacaoRetornoGuid = imagem.SelectSingleNode("id_programacao_retorno_guid")?.InnerText ?? string.Empty;
        string caminhoBackup = Path.GetRelativePath(OnedriveUtils.CaminhoOnedrive, CaminhoBackup);

        return new OrderImageProcessing(programacaoRetornoGuid, nomeImagem, caminhoBackup);
    }
    
    public Task<bool> ValidarProcessamento()
    {
        if (HashOrigem != HashDestino)
            return Task.FromResult(false);

        if (HashOrigem != HashBackup)
            return Task.FromResult(false);

        if (HashDestino != HashBackup)
            return Task.FromResult(false);

        if (!ArquivoZipValido)
            return Task.FromResult(false);

        if (!Path.Exists(CaminhoDestino))
            return Task.FromResult(false);

        if (!Path.Exists(CaminhoBackup))
            return Task.FromResult(false);
        
        ProcessamentoValido = true;
        return Task.FromResult(true);
    }

    public Task DefinirStatusOffline()
    {
        if (!ProcessamentoValido) return Task.CompletedTask;
        
        OnedriveUtils.SetOffline(CaminhoBackup);
        
        return Task.CompletedTask;
    }
    
    public virtual void Copy(string caminhoOrigem, string caminhoDestino)
    {
        string? diretorioDestino = Path.GetDirectoryName(caminhoDestino);

        if (!Directory.Exists(diretorioDestino) && diretorioDestino != null)
            Directory.CreateDirectory(path: diretorioDestino);
        
        File.Copy(caminhoOrigem, caminhoDestino, true);
        DefinirHashDestino();
    }
    
    public void Move(string caminhoOrigem, string caminhoBackup, bool fake = false)
    {
        if (HashOrigem != HashDestino)
        {
            Console.WriteLine("Falha ao validar cópia do arquivo. O arquivo não será movido.");
            return;
        }

        string? diretorioBackup = Path.GetDirectoryName(caminhoBackup);

        if (!Directory.Exists(diretorioBackup) && diretorioBackup != null)
            Directory.CreateDirectory(path: diretorioBackup);

        if (fake == false)
            File.Move(caminhoOrigem, caminhoBackup, true);
        else
            File.Copy(caminhoOrigem, caminhoBackup, true);

        DefinirHashBackup();
    }
    
    private static string GerarHashArquivo(string caminhoOrigem)
    {
        if (!Path.Exists(caminhoOrigem)) return Guid.NewGuid().ToString();
        
        using (MD5 md5 = MD5.Create())
        {
            try
            {
                using FileStream stream = File.OpenRead(caminhoOrigem);

                byte[] hash = md5.ComputeHash(stream);
                StringBuilder stringBuilder = new StringBuilder();

                foreach (byte sb in hash)
                {
                    stringBuilder.Append(sb.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }

    public override string ToString()
    {
        return $"Arquivo: {NomeSemExtensao}, Modulo: {Modulo}, Empresa: {IdEmpresa}, Guid: {Guid}, Validado: {ArquivoZipValido}, Hash: {HashOrigem}";
    }

    public bool IsSucessoProcessamento()
    {
        return ProcessamentoValido;
    }
}

public class OrderFileProcessingSmq : OrderFileProcessing
{
    public string CaminhoCorrompido { get; set; }
    public string? Avaliacao { get; private set; }
    public string? Levantamento { get; private set; }
    public uint IdBloco { get; set; }
    public bool TemDiretorioPersonalizado { get;set; }

    public OrderFileProcessingSmq(string caminhoOrigem, string diretorioDestino, string diretorioBackup, string? diretorioCorrompido, string modulo, uint idEmpresa) : base(caminhoOrigem, diretorioDestino, diretorioBackup, modulo, idEmpresa)
    {
        CaminhoCorrompido = Path.Combine(diretorioCorrompido ?? diretorioDestino, Nome);
        TemDiretorioPersonalizado = false;
    }

    public override bool ValidarArquivoZip()
    {
        byte[] zipBytes = File.ReadAllBytes(CaminhoOrigem);
        byte[] xmlBytes;

        try
        {
            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            {
                using (ZipFile zipFile = new ZipFile(zipStream))
                {
                    zipFile.Password = @"epf@1387.01#";

                    QuantidadeImagens = Convert.ToUInt16(zipFile.Cast<ZipEntry>().Count(zipEntry => zipEntry.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));

                    if (!zipFile.Cast<ZipEntry>().Any(zipEntry =>
                            zipEntry.Name.Equals("programacao.xml", StringComparison.OrdinalIgnoreCase)))
                    {
                        return ArquivoZipValido = false;
                    }

                    ZipEntry entry = zipFile.GetEntry("programacao.xml");

                    using (MemoryStream entryStream = new MemoryStream())
                    {
                        zipFile.GetInputStream(entry).CopyTo(entryStream);
                        xmlBytes = entryStream.ToArray();
                    }
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Encoding.UTF8.GetString(xmlBytes));

                XmlNode? exportacoes = xmlDoc.SelectSingleNode("//dataset/exportacao");

                IdExportacao = Convert.ToUInt16(exportacoes?.SelectSingleNode("_id")?.InnerText);

                XmlNodeList? nodes = xmlDoc.SelectNodes("//dataset/programacao_retorno");

                QuantidadeTalhoes = Convert.ToUInt16(nodes?.Count);

                foreach (XmlNode programacao in nodes)
                {
                    OrderTalhoes.Add(ProcessarTalhao(programacao));
                    IdBloco = OrderTalhoes.Select(x => x.IdBloco).Distinct().First();
                }

                XmlNodeList? nodesImagem = xmlDoc.SelectNodes("//dataset/programacao_imagem");

                if (nodesImagem == null) return ArquivoZipValido = true;

                foreach (XmlNode imagem in nodesImagem)
                {
                    OrderImagens.Add(ProcessarImagem(imagem));
                }

                return ArquivoZipValido = true;
            }
        }
        catch (ZipException)
        {
            return ArquivoZipValido = false;
        }
        catch (Exception)
        {
            return ArquivoZipValido = false;
        }

    }

    public void DefinirAvaliacao(string avaliacao)
    {
        Avaliacao = avaliacao;
    }

    public void DefinirLevantamento(string levantamento)
    {
        Levantamento = levantamento;
    }

    public void DefinirDestinoCorrompido()
    {
        CaminhoDestino = CaminhoCorrompido;
    }
    
    public void DefinirDestinoPersonalizado(List<string> pastaPersonalizada)
    {
        DateTime diaProcessamento = DateTime.Today.Date;
        (string ano, string mes, string dia) = (diaProcessamento.Year.ToString("0000"), diaProcessamento.Month.ToString("00"), diaProcessamento.Day.ToString("00"));

        if (pastaPersonalizada.Count == 1) pastaPersonalizada.Add(string.Empty);

        string pastaAvaliacao = pastaPersonalizada.First();
        string pastaLevantamento = pastaPersonalizada.Last();

        TemDiretorioPersonalizado = true;

        base.CaminhoDestino = Path.Combine(base.DiretorioDestino, pastaAvaliacao, "Avaliacao", ano, mes, dia, "Dados", pastaLevantamento, base.Nome);
    }

    public void Copy(string caminhoOrigem, string caminhoDestino, bool temDiretorioPersonalizado, List<string> pastasCriadasSessao)
    {
        string? diretorioDestino = Path.GetDirectoryName(caminhoDestino);
        string nomeArquivo = Path.GetFileName(caminhoDestino);

        if (diretorioDestino != null)
        {
            if (Directory.Exists(diretorioDestino))
            {
                if (pastasCriadasSessao.Exists(x => x == diretorioDestino))
                {
                    //...//
                }
                else
                {
                    if (TemDiretorioPersonalizado) diretorioDestino = Path.Combine(diretorioDestino, "Atrasados");
                    caminhoDestino = Path.Combine(diretorioDestino, nomeArquivo);
                }
            }
            else
            {
                pastasCriadasSessao.Add(diretorioDestino);
            }
        }

        if (diretorioDestino != null)
        {
            DiretorioDestino = diretorioDestino;
            CaminhoDestino = caminhoDestino;
            Directory.CreateDirectory(diretorioDestino);
        }

        File.Copy(caminhoOrigem, caminhoDestino, true);
        DefinirHashDestino();
    }

}