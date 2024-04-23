using Domain.Utils;

namespace Domain.Adapters;

public class OrderProcessing
{   
    public string Guid {get; init; }
    public ushort IdEmpresa { get; init; }
    public string PastaDestino { get; set; }
    public string PastaBackup { get; set; }
    public List<string> PastaOrigem { get; set; }
    public string? PastaCorrompido { get; set; }
    public string Modulo { get; init; }
    public string NomeUsuario { get; init; }
    public string NomeMaquina { get; init; }
    
    protected DateTime InicioOrder { get; init; }
    protected DateTime FimOrder { get; set; }
    
    public double ElapsedTime { get; set; }
    public ushort FilesDownloaded { get; set; }
    
    public List<OrderFileProcessing> OrderFiles { get; set; }

    public OrderProcessing(ushort idEmpresa, string pastaDestino, string pastaBackup, List<string> pastaOrigem, string modulo)
    {
        IdEmpresa = idEmpresa;
        PastaDestino = pastaDestino;
        PastaBackup = Path.Combine(OnedriveUtils.CaminhoOnedrive, pastaBackup);
        FilesDownloaded = 0;
        
        PastaOrigem = new List<string>();
        OrderFiles = new List<OrderFileProcessing>();
        foreach (string pasta in pastaOrigem) PastaOrigem.Add(Path.Combine(OnedriveUtils.CaminhoOnedrive, pasta));
                
        Modulo = modulo;

        Guid = System.Guid.NewGuid().ToString() + "-" + IdEmpresa.ToString("00") + "-" + modulo;
        NomeUsuario = Environment.UserName;
        NomeMaquina = Environment.MachineName;
        
        InicioOrder = DateTime.Now;
    }

    public bool Validate()
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
        
        List<string> arquivos = OrderFiles.Select(x => x.CaminhoBackup).ToList();
        Parallel.ForEach(arquivos, OnedriveUtils.SetOffline);
        return Task.CompletedTask;
    }

    public virtual void ProcurarArquivos(string extensao, CancellationToken cancellationToken, int maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", string.Empty);

        string[] localArquivos = PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(maxBatch).ToArray();

        List<OrderFileProcessing> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessing(arquivo, PastaDestino, PastaBackup, Modulo, IdEmpresa)));

        this.OrderFiles.AddRange(listaArquivos);
    }

    public void FinishOrder()
    {
        FimOrder = DateTime.Now;
        ElapsedTime = (FimOrder - InicioOrder).TotalSeconds;
    }
};

public class OrderProcessingSmq : OrderProcessing
{
    public OrderProcessingSmq(ushort idEmpresa, string pastaDestino, string pastaBackup, List<string> pastaOrigem, string modulo) : base(idEmpresa, pastaDestino, pastaBackup, pastaOrigem, modulo)
    {
        IdEmpresa = idEmpresa;
        PastaDestino = pastaDestino;
        PastaBackup = Path.Combine(OnedriveUtils.CaminhoOnedrive, pastaBackup);
        FilesDownloaded = 0;

        PastaOrigem = new List<string>();
        OrderFiles = new List<OrderFileProcessing>();
        foreach (string pasta in pastaOrigem) PastaOrigem.Add(Path.Combine(OnedriveUtils.CaminhoOnedrive, pasta));

        Modulo = modulo;

        Guid = System.Guid.NewGuid().ToString() + "-" + IdEmpresa.ToString("00") + "-" + modulo;
        NomeUsuario = Environment.UserName;
        NomeMaquina = Environment.MachineName;

        InicioOrder = DateTime.Now;
    }

    public override void ProcurarArquivos(string extensao, CancellationToken cancellationToken, int maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", string.Empty);

        string[] localArquivos = PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(maxBatch).ToArray();

        List<OrderFileProcessingSmq> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessingSmq(arquivo, PastaDestino, PastaBackup, PastaCorrompido, Modulo, IdEmpresa)));

        this.OrderFiles.AddRange(listaArquivos);
    }
}
