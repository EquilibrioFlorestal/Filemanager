using Domain.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Serilog;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace Domain.Adapters;

public class OrderFileProcessing : IDisposable
{
    public Guid Guid { get; init; }
    public String Nome { get; set; }
    public String NomeSemExtensao { get; set; }
    public String Extensao { get; set; }
    public Int64 TamanhoBytes { get; init; }

    protected UInt32 IdEmpresa { get; set; }
    protected String Modulo { get; set; }
    protected UInt32 IdExportacao { get; set; }
    public UInt32 IdCiclo { get; set; }

    public UInt16 QuantidadeImagens { get; set; }
    public UInt16 QuantidadeTalhoes { get; set; }

    public String CaminhoOrigem { get; set; }
    public String CaminhoDestino { get; set; }
    public String CaminhoBackup { get; set; }

    public String DiretorioDestino { get; set; }
    public String DiretorioBackup { get; set; }

    protected String? HashOrigem { get; set; }
    protected String? HashDestino { get; set; }
    protected String? HashBackup { get; set; }

    protected Boolean ArquivoZipValido { get; set; }
    protected Boolean ProcessamentoValido { get; set; }

    public DateTime DataProcessamento { get; init; }

    public List<OrderImageProcessing> OrderImagens { get; set; }
    public List<OrderTalhaoProcessing> OrderTalhoes { get; set; }

    private Boolean disposed = false;

    public OrderFileProcessing() { }

    public OrderFileProcessing(String caminhoOrigem, String diretorioDestino, String diretorioBackup, String modulo, UInt32 idEmpresa)
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
        String hashFile = GerarHashArquivo(CaminhoOrigem);
        HashOrigem = hashFile;
    }

    protected void DefinirHashDestino()
    {
        String hashFile = GerarHashArquivo(CaminhoDestino);
        HashDestino = hashFile;
    }

    protected void DefinirHashBackup()
    {
        String hashFile = GerarHashArquivo(CaminhoBackup);
        HashBackup = hashFile;
    }

    public virtual Boolean ValidarArquivoZip()
    {
        try
        {
            Byte[] zipBytes = File.ReadAllBytes(CaminhoOrigem);
            Byte[] xmlBytes;

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
            Log.Warning("Arquivo zip não é válido");
            return ArquivoZipValido = false;
        }
        catch (IOException)
        {
            Log.Warning("Arquivo zip está em uso por outro processo");
            throw new IOException("because it is being used by another process");
        }
        catch (Exception)
        {
            Log.Warning("Arquivo zip possui erro desconhecido");
            return ArquivoZipValido = false;
        }
    }

    protected OrderTalhaoProcessing ProcessarTalhao(XmlNode talhao)
    {
        UInt32 _idTipoLevantamento = Convert.ToUInt32(talhao.SelectSingleNode("id_tipo_levantamento")?.InnerText ?? String.Empty);
        OrderTalhaoProcessing order = new OrderTalhaoProcessing
        {
            DataSituacao = DateTime.ParseExact(talhao.SelectSingleNode("dt_situacao")?.InnerText ?? String.Empty,
                "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Id = Convert.ToUInt32(talhao.SelectSingleNode("_id")?.InnerText ?? String.Empty),
            IdArea = Convert.ToUInt32(talhao.SelectSingleNode("id_area_emp")?.InnerText ?? String.Empty),
            IdBloco = Convert.ToUInt32(talhao.SelectSingleNode("id_bloco")?.InnerText ?? String.Empty),

            Latitude = talhao.SelectSingleNode("vl_latitude")?.InnerText ?? String.Empty,
            Longitude = talhao.SelectSingleNode("vl_longitude")?.InnerText ?? String.Empty,
            Altitude = talhao.SelectSingleNode("vl_altitude")?.InnerText ?? String.Empty,
            Direcao = talhao.SelectSingleNode("vl_direcao")?.InnerText ?? String.Empty,
            Precisao = talhao.SelectSingleNode("vl_precisao")?.InnerText ?? String.Empty,

            IdTipoLevantamento = ( _idTipoLevantamento == 102 ) ? 101u : _idTipoLevantamento,
            Modulo = Modulo.ToString(),
            IdCiclo = IdCiclo.ToString(),
            ImeiColetor = talhao.SelectSingleNode("cd_imei_situacao")?.InnerText.ToUpper() ?? String.Empty,
            NomeArquivo = Nome.ToString(),

            ProgramacaoGuid = talhao.SelectSingleNode("id_programacao_guid")?.InnerText ?? String.Empty,
            ProgramacaoRetornoGuid = talhao.SelectSingleNode("id_programacao_retorno_guid")?.InnerText ?? String.Empty,

            Motivo = talhao.SelectSingleNode("ds_motivo_obs")?.InnerText ?? String.Empty,
            IdEmpresa = IdEmpresa,
            Observacao = talhao.SelectSingleNode("ds_obs")?.InnerText ?? String.Empty,
            IdEquipe = Convert.ToUInt16(talhao.SelectSingleNode("id_equipe_situacao")?.InnerText),
            IdExportacao = IdExportacao,
            IdMotivo = Convert.ToUInt16(talhao.SelectSingleNode("id_motivo")?.InnerText),
            IdSituacao = Convert.ToUInt16(talhao.SelectSingleNode("id_situacao")?.InnerText),
            IdUsuario = Convert.ToUInt16(talhao.SelectSingleNode("id_usuario_situacao")?.InnerText),
            SnNovo = talhao.SelectSingleNode("sn_novo")?.InnerText.ToCharArray().FirstOrDefault(),
        };
        return order;
    }

    protected OrderImageProcessing ProcessarImagem(XmlNode imagem)
    {
        String nomeImagem = imagem.SelectSingleNode("no_imagem")?.InnerText ?? String.Empty;
        String programacaoRetornoGuid = imagem.SelectSingleNode("id_programacao_retorno_guid")?.InnerText ?? String.Empty;
        String caminhoBackup = Path.GetRelativePath(OnedriveUtils.CaminhoOnedrive, CaminhoBackup);

        return new OrderImageProcessing(programacaoRetornoGuid, nomeImagem, caminhoBackup);
    }

    public Task<Boolean> ValidarProcessamento()
    {
        if (HashOrigem != HashDestino)
        {
            Log.Warning("HashOrigem != HashDestino");
            return Task.FromResult(false);
        }

        if (HashOrigem != HashBackup)
        {
            Log.Warning("HashOrigem != HashBackup");
            return Task.FromResult(false);
        }

        if (HashDestino != HashBackup)
        {
            Log.Warning("HashDestino != HashBackup");
            return Task.FromResult(false);
        }

        if (!ArquivoZipValido)
        {
            Log.Warning("Arquivo zip inválido");
            return Task.FromResult(false);
        }

        if (!Path.Exists(CaminhoDestino))
        {
            Log.Warning("CaminhoDestino não existe.");
            return Task.FromResult(false);
        }

        if (!Path.Exists(CaminhoBackup))
        {
            Log.Warning("CaminhoBackup não existe.");
            return Task.FromResult(false);
        }

        Log.Information("Processamento validado");
        ProcessamentoValido = true;
        return Task.FromResult(true);
    }

    public Task DefinirStatusOffline()
    {
        if (!ProcessamentoValido) return Task.CompletedTask;

        OnedriveUtils.SetOffline(CaminhoBackup);

        return Task.CompletedTask;
    }

    public virtual void Copy(String caminhoOrigem, String caminhoDestino)
    {
        String? diretorioDestino = Path.GetDirectoryName(caminhoDestino);

        if (!Directory.Exists(diretorioDestino) && diretorioDestino != null)
            Directory.CreateDirectory(path: diretorioDestino);

        try
        {
            File.Copy(caminhoOrigem, caminhoDestino, true);
        }
        catch (Exception ex)
        {
            Log.Warning($"Falha ao copiar arquivo: {ex.Message}");
        }

        DefinirHashDestino();
    }

    public void Move(String caminhoOrigem, String caminhoBackup, Boolean fake = false)
    {
        if (HashOrigem != HashDestino)
        {
            Console.WriteLine("Falha ao validar cópia do arquivo. O arquivo não será movido.");
            return;
        }

        String? diretorioBackup = Path.GetDirectoryName(caminhoBackup);

        if (!Directory.Exists(diretorioBackup) && diretorioBackup != null)
            Directory.CreateDirectory(path: diretorioBackup);

        try
        {
            if (fake == false)
                File.Move(caminhoOrigem, caminhoBackup, true);
            else
                File.Copy(caminhoOrigem, caminhoBackup, true);
        }
        catch (Exception ex)
        {
            Log.Warning($"Falha ao mover arquivo: {ex.Message}");
        }

        DefinirHashBackup();
    }

    private static String GerarHashArquivo(String caminhoOrigem)
    {
        if (!Path.Exists(caminhoOrigem)) return Guid.NewGuid().ToString();

        using (MD5 md5 = MD5.Create())
        {
            try
            {
                using FileStream stream = File.OpenRead(caminhoOrigem);

                Byte[] hash = md5.ComputeHash(stream);
                StringBuilder stringBuilder = new StringBuilder();

                foreach (Byte sb in hash)
                {
                    stringBuilder.Append(sb.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Warning($"Não foi possível gerar a hash: {ex.Message}");
                throw;
            }
        }
    }

    public override String ToString()
    {
        return $"Arquivo: {NomeSemExtensao}, Modulo: {Modulo}, Empresa: {IdEmpresa}, Guid: {Guid}, Validado: {ArquivoZipValido}, Hash: {HashOrigem}";
    }

    public Boolean IsSucessoProcessamento()
    {
        return ProcessamentoValido;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(Boolean disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                OrderImagens.Clear();
                OrderTalhoes.Clear();

                HashOrigem = null;
                HashDestino = null;
                HashBackup = null;

                CaminhoBackup = String.Empty;
                CaminhoDestino = String.Empty;
                CaminhoOrigem = String.Empty;
            }

            disposed = true;
        }
    }

    ~OrderFileProcessing()
    {
        Dispose(false);
    }

}

public class OrderFileProcessingSmq : OrderFileProcessing
{
    public String CaminhoCorrompido { get; set; }
    public String? Avaliacao { get; private set; }
    public String? Levantamento { get; private set; }
    public UInt32 IdBloco { get; set; }
    public Boolean TemDiretorioPersonalizado { get; set; }

    public OrderFileProcessingSmq(String caminhoOrigem, String diretorioDestino, String diretorioBackup, String? diretorioCorrompido, String modulo, UInt32 idEmpresa) : base(caminhoOrigem, diretorioDestino, diretorioBackup, modulo, idEmpresa)
    {
        CaminhoCorrompido = Path.Combine(diretorioCorrompido ?? diretorioDestino, Nome);
        TemDiretorioPersonalizado = false;
    }

    public override Boolean ValidarArquivoZip()
    {
        Byte[] zipBytes = File.ReadAllBytes(CaminhoOrigem);
        Byte[] xmlBytes;

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
            Log.Warning("Arquivo zip não é válido");
            return ArquivoZipValido = false;
        }
        catch (IOException)
        {
            Log.Warning("Arquivo zip está em uso por outro processo");
            throw new IOException("because it is being used by another process");
        }
        catch (Exception)
        {
            Log.Warning("Arquivo zip possui erro desconhecido");
            return ArquivoZipValido = false;
        }

    }

    public void DefinirAvaliacao(String avaliacao)
    {
        Avaliacao = avaliacao;
    }

    public void DefinirLevantamento(String levantamento)
    {
        Levantamento = levantamento;
    }

    public void DefinirDestinoCorrompido()
    {
        CaminhoDestino = CaminhoCorrompido;
    }

    public void DefinirDestinoPersonalizado(List<String> pastaPersonalizada)
    {
        DateTime diaProcessamento = DateTime.Today.Date;
        (String ano, String mes, String dia) = (diaProcessamento.Year.ToString("0000"), diaProcessamento.Month.ToString("00"), diaProcessamento.Day.ToString("00"));

        if (pastaPersonalizada.Count == 1) pastaPersonalizada.Add(String.Empty);

        String pastaAvaliacao = pastaPersonalizada.First();
        String pastaLevantamento = pastaPersonalizada.Last();

        TemDiretorioPersonalizado = true;

        base.CaminhoDestino = Path.Combine(base.DiretorioDestino, pastaAvaliacao, "Avaliacao", ano, mes, dia, "Dados", pastaLevantamento, base.Nome);
    }

    public void Copy(String caminhoOrigem, String caminhoDestino, Boolean temDiretorioPersonalizado, List<String> pastasCriadasSessao)
    {
        String? diretorioDestino = Path.GetDirectoryName(caminhoDestino);
        String nomeArquivo = Path.GetFileName(caminhoDestino);

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