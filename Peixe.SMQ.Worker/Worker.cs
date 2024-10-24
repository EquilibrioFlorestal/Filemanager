using Domain.Adapters;
using Domain.Constants;
using Domain.Interfaces;
using Domain.Utils;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;

namespace Peixe.SMQ.Worker;

public class Worker(IConfiguration configuration, IServiceProvider serviceProvider, IHostApplicationLifetime host) : BackgroundService
{
    private readonly IConfiguration _configuration = configuration;
    private static readonly Queue<OrderProcessing?> FilaRequisicoes = new Queue<OrderProcessing?>();
    private static readonly List<String> PastasCriadasSessao = new List<String>();
    private static readonly Object LockObj = new Object();
    private const String Extensao = ".zip";
    private const String FilenameOrders = "requests.json";
    private UInt16 _delaySecondsEachRequest = 10;
    private UInt16 _maxBatchTask = 20;
    private UInt16 _delayHoursEachBackgroundTagOffline = 8;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!OnedriveUtils.CheckProcessOnedrive())
        {
            Exception ex = new Exception("No Onedrive process was found.");
            AnsiConsole.WriteException(ex);
            host.StopApplication();
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Core[/]: {Environment.ProcessorCount} núcleos ativos.");

        Task verificarFilaTask = Task.Run(() => VerificarFilaTarefasAsync(cancellationToken), cancellationToken);

        try
        {
            lock (LockObj)
            {
                CarregarConfiguracoesJson();
                AdicionarTarefa(cancellationToken);
            }
        }
        catch (Exception)
        {
            Log.Warning("Não foi possível obter preferência");
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

    }

    void CarregarConfiguracoesJson()
    {
        try
        {
            IConfigurationSection config = _configuration.GetSection("Peixe");
            UInt16 loadDelay = config.GetValue<UInt16>("delaySecondsTask");
            UInt16 loadBatch = config.GetValue<UInt16>("maxBatchTask");
            UInt16 loadDelayBackgroundTagOffline = config.GetValue<UInt16>("delayHoursBackgroundTagOffline");

            if (loadDelayBackgroundTagOffline != _delayHoursEachBackgroundTagOffline)
            {
                //AnsiConsole.MarkupLine($"[cyan]Delay[/]: tarefa em segundo plano ajustada para {loadDelayBackgroundTagOffline} horas.");
                _delayHoursEachBackgroundTagOffline = loadDelayBackgroundTagOffline;
            }

            if (loadDelay != _delaySecondsEachRequest)
            {
                //AnsiConsole.MarkupLine($"[cyan]Delay[/]: ajustado para {loadDelay} segundos.");
                _delaySecondsEachRequest = loadDelay;
            }

            if (loadBatch != _maxBatchTask)
            {
                //AnsiConsole.MarkupLine($"[cyan]Batch[/]: ajustado para {loadBatch} arquivos.");
                _maxBatchTask = loadBatch;
            }

            Log.Debug("Configurações carregadas com sucesso");

        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine("[red]Configuracao[/]: Falha ao ler Tag [[Peixe.delaySeconds]] do arquivo de configuracoes.");
        }
    }

    void AdicionarTarefa(CancellationToken cancellationToken)
    {
        try
        {
            lock (LockObj)
            {
                List<OrderProcessing>? orders = LerRequisicoesJson();

                orders?.ForEach(tarefa =>
                {
                    if (!tarefa.Validate())
                    {
                        AnsiConsole.MarkupLine($"[white on red]Tarefa: {tarefa.Guid} nao e valida.[/]");
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested) return;

                    FilaRequisicoes.Enqueue(tarefa);
                });
            }
        }
        catch (Exception)
        {
            Log.Warning("Não foi possível obter preferência");
        }
    }

    List<OrderProcessing>? LerRequisicoesJson()
    {
        String caminhoArquivoOrders = Path.Combine(Directory.GetCurrentDirectory(), FilenameOrders);
        if (!Path.Exists(caminhoArquivoOrders))
        {
            AnsiConsole.MarkupLine($"[red]Configuracao[/]: Arquivo de configuracao {FilenameOrders} ausente.");
            throw new FileNotFoundException();
        }

        String contentYaml = File.ReadAllText(caminhoArquivoOrders);
        List<OrderProcessing>? orders = JsonConvert.DeserializeObject<List<OrderProcessing>>(contentYaml);

        Log.Debug($"{orders.Count} Requisições carregadas");
        return orders;
    }

    void VerificarFilaTarefasAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {

                lock (LockObj)
                {
                    Log.Debug($"Requisições {FilaRequisicoes.Count}");

                    if (FilaRequisicoes.Count <= 0) return;

                    OrderProcessing? requisicao = FilaRequisicoes.Dequeue();

                    if (requisicao == null) continue;

                    Log.Debug($"Iniciando processamento da tarefa {requisicao.Guid}");

                    ProcessarTarefa(requisicao, cancellationToken).Wait();

                    AnsiConsole.MarkupLine($"[green]Tarefa[/]: Concluida {requisicao.Guid} [[Arquivos: {requisicao.FilesDownloaded}]]");

                    AnsiConsole.WriteLine();

                    Int32 quantidadeSucesso = requisicao.OrderFiles.Where(x => x.IsSucessoProcessamento() == true).Count();

                    AnsiConsole.Write(
                        new BarChart()
                        .Width(60)
                        .Label("Resumo transações")
                        .CenterLabel()
                        .AddItem("Sucesso", quantidadeSucesso, Color.Green)
                        .AddItem("Falha", requisicao.OrderFiles.Count - quantidadeSucesso, Color.Red)
                        );


                    host.StopApplication();
                }
            }
            catch (Exception)
            {
                Log.Warning("Não foi possível obter preferência");
            }
        }
    }

    async Task ProcessarTarefa(OrderProcessing requisicao, CancellationToken cancellationToken)
    {
        requisicao.OrderFiles.AddRange(ProcurarArquivos(requisicao, Extensao, cancellationToken, _maxBatchTask));

        if (cancellationToken.IsCancellationRequested) return;

        AnsiConsole.Progress()
            .HideCompleted(false)
            .AutoClear(true)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn().CompletedStyle(new Style(background: Color.Cyan1))
            })
            .Start(ctx =>
            {
                ProgressTask task1 = ctx.AddTask("[green]Arquivo[/]: Processando arquivo");

                task1.MaxValue = requisicao.OrderFiles.Count;

                while (!ctx.IsFinished)
                {
                    foreach (OrderFileProcessingSmq requisicaoArquivo in requisicao.OrderFiles)
                    {
                        ProcessarArquivo(requisicao, requisicaoArquivo).Wait();
                        task1.Increment(1);
                    }
                }
            });

        requisicao.FinishOrder();
        await requisicao.DefinirStatusOffline();
    }

    async Task ProcessarArquivo(OrderProcessing requisicao, OrderFileProcessingSmq requisicaoArquivo)
    {
        Boolean arquivoZipValido = requisicaoArquivo.ValidarArquivoZip();

        if (!arquivoZipValido)
        {
            AnsiConsole.MarkupLine($"[white on red]Arquivo: {requisicaoArquivo.NomeSemExtensao} esta corrompido !![/]");
            requisicaoArquivo.DefinirDestinoCorrompido();
        }
        else
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IBlocoService service = scope.ServiceProvider.GetRequiredService<IBlocoService>();

                String bloco = await service.ListarBloco(requisicaoArquivo.IdBloco, requisicaoArquivo.IdCiclo);

                if (!bloco.IsNullOrEmpty())
                {
                    List<String>? pastaCustomizada = Avaliacao.NomePastaAvaliacao.Where(x => x.Key == bloco).Select(x => x.Value).FirstOrDefault();
                    if (pastaCustomizada != null) requisicaoArquivo.DefinirDestinoPersonalizado(pastaCustomizada);
                }
                else
                {
                    requisicaoArquivo.DefinirDestinoCorrompido();
                }
            }
        }

        requisicaoArquivo.Copy(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoDestino, requisicaoArquivo.TemDiretorioPersonalizado, PastasCriadasSessao);
        requisicaoArquivo.Move(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoBackup);

        Boolean validadeProcessamento = await requisicaoArquivo.ValidarProcessamento();

        requisicao.FilesDownloaded += 1;

        if (!validadeProcessamento && arquivoZipValido)
        {
            AnsiConsole.MarkupLine($"[red]Transferencia[/]: {requisicaoArquivo.NomeSemExtensao}");
            return;
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IArquivoService service = scope.ServiceProvider.GetRequiredService<IArquivoService>();

            Boolean jaCadastrado = await service.VerificarCadastrado(requisicaoArquivo.Nome, requisicao.Modulo, requisicao.IdEmpresa);

            if (!jaCadastrado) await service.CadastrarArquivo(requisicao, requisicaoArquivo);
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ITalhaoService service = scope.ServiceProvider.GetRequiredService<ITalhaoService>();

            foreach (OrderTalhaoProcessing orderTalhao in requisicaoArquivo.OrderTalhoes)
            {
                Boolean jaCadastrado = await service.VerificarCadastrado(orderTalhao.NomeArquivo, orderTalhao.ProgramacaoRetornoGuid);

                if (!jaCadastrado) await service.CadastrarTalhao(orderTalhao);
            }
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IImagemService service = scope.ServiceProvider.GetRequiredService<IImagemService>();

            foreach (OrderImageProcessing orderImagem in requisicaoArquivo.OrderImagens)
            {
                Boolean jaCadastrado = await service.VerificarCadastrado(orderImagem.NomeImagem, orderImagem.ProgramacaoRetornoGuid);

                if (!jaCadastrado) await service.CadastrarImagem(orderImagem);
            }
        }

    }

    private List<OrderFileProcessingSmq> ProcurarArquivos(OrderProcessing requisicao, String extensao, CancellationToken cancellationToken, UInt16? maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", String.Empty);

        List<String> localArquivos = requisicao.PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{requisicao.Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(_maxBatchTask).ToList();

        List<OrderFileProcessingSmq> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessingSmq(arquivo, requisicao.PastaDestino, requisicao.PastaBackup, requisicao.PastaCorrompido, requisicao.Modulo, requisicao.IdEmpresa)));

        return listaArquivos;
    }

}
