using System.Globalization;
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
    public Guid Guid { get; set; }
    public string Nome { get; set; }
    public string NomeSemExtensao { get; set; }
    public string Extensao { get; set; }
    public long TamanhoBytes { get; set; }

    private uint IdEmpresa { get; set; }
    private string Modulo { get; set; }
    private uint IdExportacao { get; set; }
    private uint IdCiclo { get; set; }
    
    public ushort QuantidadeImagens { get; set; }
    public ushort QuantidadeTalhoes { get; set; }
    
    public string CaminhoOrigem { get; set; }
    public string CaminhoDestino { get; set; }
    public string CaminhoBackup { get; set; }

    private string? HashOrigem { get; set; }
    private string? HashDestino { get; set; }
    private string? HashBackup { get; set; }

    private bool ArquivoZipValido { get; set; }
    public DateTime DataProcessamento { get; set; }

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
        
        Regex regexIdCiclo = new Regex(@"DICE_(\d+)_.*");
        IdCiclo = Convert.ToUInt16(regexIdCiclo.Match(Nome).Groups[1].Value);
        CaminhoOrigem = caminhoOrigem;
        CaminhoDestino = Path.Combine(diretorioDestino, Nome);
        CaminhoBackup = Path.Combine(diretorioBackup, Nome);

        DataProcessamento = DateTime.Now;

        OrderImagens = new List<OrderImageProcessing>();
        OrderTalhoes = new List<OrderTalhaoProcessing>();
        
        SetHashOrigem();
    }

    private void SetHashOrigem()
    {
        string hashFile = GenerateHashFile(CaminhoOrigem);
        HashOrigem = hashFile;
    }

    private void SetHashDestino()
    {
        string hashFile = GenerateHashFile(CaminhoDestino);
        HashDestino = hashFile;
    }

    private void SetHashBackup()
    {
        string hashFile = GenerateHashFile(CaminhoBackup);
        HashBackup = hashFile;
    }

    public bool ValidarArquivoZip()
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

    private OrderTalhaoProcessing ProcessarTalhao(XmlNode talhao)
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

    private OrderImageProcessing ProcessarImagem(XmlNode imagem)
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

        return Task.FromResult(true);
    }

    public void Copy(string caminhoOrigem, string caminhoDestino)
    {
        File.Copy(caminhoOrigem, caminhoDestino, true);
        SetHashDestino();
    }

    public async Task CopyAsync(string caminhoOrigem, string caminhoDestino)
    {        
        await Task.Run(() => File.Copy(caminhoOrigem, caminhoDestino, true));
        SetHashDestino();
    }

    public void Move(string caminhoOrigem, string caminhoBackup)
    {
        if (HashOrigem != HashDestino)
        {
            Console.WriteLine("Falha ao validar cópia do arquivo. O arquivo não será movido.");
            return;
        }
        
        File.Move(caminhoOrigem, caminhoBackup, true);
        SetHashBackup();
    }

    public async Task MoveAsync(string caminhoOrigem, string caminhoBackup)
    {
        if (HashOrigem != HashDestino)
        {
            Console.WriteLine("Falha ao validar cópia do arquivo. O arquivo não será movido.");
            return;
        }

        await Task.Run(() => File.Move(caminhoOrigem, caminhoBackup, true));
        SetHashBackup();
    }

    private static string GenerateHashFile(string caminhoOrigem)
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
}