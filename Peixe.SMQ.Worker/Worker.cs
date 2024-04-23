using Domain.Adapters;
using Domain.Constants;
using Domain.Interfaces;
using Domain.Utils;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Spectre.Console;

namespace Peixe.SMQ.Worker
{
    public class Worker(IConfiguration configuration, IServiceProvider serviceProvider, IHostApplicationLifetime host) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private static readonly Queue<OrderProcessingSmq?> FilaRequisicoes = new();
        private static readonly List<string> PastasCriadasSessao = new();
        private static readonly object LockObj = new object();
        private const string Extensao = ".zip";
        private const string FilenameOrders = "requests.json";
        private ushort _delaySecondsEachRequest = 10;
        private ushort _maxBatchTask = 20;
        private ushort _delayHoursEachBackgroundTagOffline = 8;

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

            lock (LockObj)
            {
                CarregarConfiguracoesJson();

                AdicionarTarefa(cancellationToken);
            }

            await verificarFilaTask;

        }

        void CarregarConfiguracoesJson()
        {
            try
            {
                IConfigurationSection config = _configuration.GetSection("Peixe");
                ushort loadDelay = config.GetValue<ushort>("delaySecondsTask");
                ushort loadBatch = config.GetValue<ushort>("maxBatchTask");
                ushort loadDelayBackgroundTagOffline = config.GetValue<ushort>("delayHoursBackgroundTagOffline");

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
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]Configuracao[/]: Falha ao ler Tag [[Peixe.delaySeconds]] do arquivo de configuracoes.");
            }
        }

        void AdicionarTarefa(CancellationToken cancellationToken)
        {
            lock (LockObj)
            {
                List<OrderProcessingSmq>? orders = LerRequisicoesJson();

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

        List<OrderProcessingSmq>? LerRequisicoesJson()
        {
            string caminhoArquivoOrders = Path.Combine(Directory.GetCurrentDirectory(), FilenameOrders);
            if (!Path.Exists(caminhoArquivoOrders))
            {
                AnsiConsole.MarkupLine($"[red]Configuracao[/]: Arquivo de configuracao {FilenameOrders} ausente.");
                throw new FileNotFoundException();
            }

            string contentYaml = File.ReadAllText(caminhoArquivoOrders);
            List<OrderProcessingSmq>? orders = JsonConvert.DeserializeObject<List<OrderProcessingSmq>>(contentYaml);
            return orders;
        }

        void VerificarFilaTarefasAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (LockObj)
                {
                    if (FilaRequisicoes.Count <= 0)
                    {
                        host.StopApplication();
                        return;
                    }

                    OrderProcessingSmq? requisicao = FilaRequisicoes.Dequeue();

                    if (requisicao == null) continue;

                    ProcessarTarefa(requisicao, cancellationToken).Wait();

                    AnsiConsole.MarkupLine($"[green]Tarefa[/]: Concluida {requisicao.Guid} [[Arquivos: {requisicao.FilesDownloaded}]]");

                    AnsiConsole.WriteLine();

                    var quantidadeSucesso = requisicao.OrderFiles.Where(x => x.IsSucessoProcessamento() == true).Count();

                    AnsiConsole.Write(
                        new BarChart()
                        .Width(60)
                        .Label("Resumo transações")
                        .CenterLabel()
                        .AddItem("Sucesso", quantidadeSucesso, Color.Green)
                        .AddItem("Falha", requisicao.OrderFiles.Count - quantidadeSucesso, Color.Red)
                        );
                }
            }
        }

        async Task ProcessarTarefa(OrderProcessing requisicao, CancellationToken cancellationToken)
        {
            requisicao.ProcurarArquivos(Extensao, cancellationToken, _maxBatchTask);

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
                    var task1 = ctx.AddTask("[green]Arquivo[/]: Processando arquivo");

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
            bool arquivoZipValido = requisicaoArquivo.ValidarArquivoZip();

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

                    string bloco = await service.ListarBloco(requisicaoArquivo.IdBloco, requisicaoArquivo.IdCiclo);

                    if (!bloco.IsNullOrEmpty())
                    {
                        List<string>? pastaCustomizada = Avaliacao.NomePastaAvaliacao.Where(x => x.Key == bloco).Select(x => x.Value).FirstOrDefault();
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

            bool validadeProcessamento = await requisicaoArquivo.ValidarProcessamento();

            requisicao.FilesDownloaded += 1;

            if (!validadeProcessamento && arquivoZipValido)
            {
                AnsiConsole.MarkupLine($"[red]Transferencia[/]: {requisicaoArquivo.NomeSemExtensao}");
                return;
            }

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IArquivoService service = scope.ServiceProvider.GetRequiredService<IArquivoService>();

                bool jaCadastrado = await service.VerificarCadastrado(requisicaoArquivo.Nome, requisicao.Modulo, requisicao.IdEmpresa);

                if (!jaCadastrado) await service.CadastrarArquivo(requisicao, requisicaoArquivo);
            }

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                ITalhaoService service = scope.ServiceProvider.GetRequiredService<ITalhaoService>();

                foreach (OrderTalhaoProcessing orderTalhao in requisicaoArquivo.OrderTalhoes)
                {
                    bool jaCadastrado = await service.VerificarCadastrado(orderTalhao.NomeArquivo, orderTalhao.ProgramacaoRetornoGuid);

                    if (!jaCadastrado) await service.CadastrarTalhao(orderTalhao);
                }
            }

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IImagemService service = scope.ServiceProvider.GetRequiredService<IImagemService>();

                foreach (OrderImageProcessing orderImagem in requisicaoArquivo.OrderImagens)
                {
                    bool jaCadastrado = await service.VerificarCadastrado(orderImagem.NomeImagem, orderImagem.ProgramacaoRetornoGuid);

                    if (!jaCadastrado) await service.CadastrarImagem(orderImagem);
                }
            }

        }

    }
}
