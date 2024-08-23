using Domain.Utils;

namespace Domain.Adapters;

public class OrderProcessing
{
    public String Guid { get; init; }
    public UInt16 IdEmpresa { get; init; }
    public String PastaDestino { get; set; }
    public String PastaBackup { get; set; }
    public List<String> PastaOrigem { get; set; }
    public String? PastaCorrompido { get; set; }
    public String Modulo { get; init; }
    public String NomeUsuario { get; init; }
    public String NomeMaquina { get; init; }

    protected DateTime InicioOrder { get; init; }
    protected DateTime FimOrder { get; set; }

    public Double ElapsedTime { get; set; }
    public UInt16 FilesDownloaded { get; set; }

    public List<OrderFileProcessing> OrderFiles { get; set; }

    public OrderProcessing(UInt16 idEmpresa, String pastaDestino, String pastaBackup, List<String> pastaOrigem, String modulo)
    {
        IdEmpresa = idEmpresa;
        PastaDestino = pastaDestino;
        PastaBackup = Path.Combine(OnedriveUtils.CaminhoOnedrive, pastaBackup);
        FilesDownloaded = 0;

        PastaOrigem = new List<String>();
        OrderFiles = new List<OrderFileProcessing>();
        foreach (String pasta in pastaOrigem) PastaOrigem.Add(Path.Combine(OnedriveUtils.CaminhoOnedrive, pasta));

        Modulo = modulo;

        Guid = System.Guid.NewGuid().ToString() + "-" + IdEmpresa.ToString("00") + "-" + modulo;
        NomeUsuario = Environment.UserName;
        NomeMaquina = Environment.MachineName;

        InicioOrder = DateTime.Now;
    }

    public Boolean Validate()
    {
        if (!Directory.Exists(PastaDestino))
        {
            Console.WriteLine($"Caminho: {PastaDestino} não encontrada.");
            return false;
        }
        if (!Directory.Exists(PastaBackup))
        {
            Console.WriteLine($"Caminho: {PastaBackup} não encontrada.");
            return false;
        }
        if (!PastaOrigem.All(x => Directory.Exists(x)))
        {
            Console.WriteLine($"Caminho: Caminho Origem não encontrada.");
            return false;
        };

        return true;
    }

    public Task DefinirStatusOffline()
    {
        if (FilesDownloaded <= 0) return Task.CompletedTask;

        List<String> arquivos = OrderFiles.Select(x => x.CaminhoBackup).ToList();
        Parallel.ForEach(arquivos, OnedriveUtils.SetOffline);
        return Task.CompletedTask;
    }

    public virtual void ProcurarArquivos(String extensao, CancellationToken cancellationToken, Int32 maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", String.Empty);

        String[] localArquivos = PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(maxBatch).ToArray();

        List<OrderFileProcessing> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessing(arquivo, PastaDestino, PastaBackup, Modulo, IdEmpresa)));

        this.OrderFiles.AddRange(listaArquivos);
    }

    public void FinishOrder()
    {
        FimOrder = DateTime.Now;
        ElapsedTime = ( FimOrder - InicioOrder ).TotalSeconds;
    }
};

public class OrderProcessingSmq : OrderProcessing
{
    public OrderProcessingSmq(UInt16 idEmpresa, String pastaDestino, String pastaBackup, List<String> pastaOrigem, String modulo) : base(idEmpresa, pastaDestino, pastaBackup, pastaOrigem, modulo)
    {
        IdEmpresa = idEmpresa;
        PastaDestino = pastaDestino;
        PastaBackup = Path.Combine(OnedriveUtils.CaminhoOnedrive, pastaBackup);
        FilesDownloaded = 0;

        PastaOrigem = new List<String>();
        OrderFiles = new List<OrderFileProcessing>();
        foreach (String pasta in pastaOrigem) PastaOrigem.Add(Path.Combine(OnedriveUtils.CaminhoOnedrive, pasta));

        Modulo = modulo;

        Guid = System.Guid.NewGuid().ToString() + "-" + IdEmpresa.ToString("00") + "-" + modulo;
        NomeUsuario = Environment.UserName;
        NomeMaquina = Environment.MachineName;

        InicioOrder = DateTime.Now;
    }

    public override void ProcurarArquivos(String extensao, CancellationToken cancellationToken, Int32 maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", String.Empty);

        String[] localArquivos = PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(maxBatch).ToArray();

        List<OrderFileProcessingSmq> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessingSmq(arquivo, PastaDestino, PastaBackup, PastaCorrompido, Modulo, IdEmpresa)));

        this.OrderFiles.AddRange(listaArquivos);
    }
}
