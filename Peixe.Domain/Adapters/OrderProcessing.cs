using Domain.Utils;

namespace Domain.Adapters;

public class OrderProcessing
{   
    public string Guid {get; set; }
    public ushort IdEmpresa { get; set; }
    public string PastaDestino { get; set; }
    public string PastaBackup { get; set; }
    public List<string> PastaOrigem { get; set; }
    public string? PastaCorrompido { get; set; }
    public string Modulo { get; set; }
    public string NomeUsuario { get; set; }
    public string NomeMaquina { get; set; }
    
    private DateTime InicioOrder { get; set; }
    private DateTime FimOrder { get; set; }
    
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
    
    public void FinishOrder()
    {
        FimOrder = DateTime.Now;
        ElapsedTime = (FimOrder - InicioOrder).TotalSeconds;
    }
};
