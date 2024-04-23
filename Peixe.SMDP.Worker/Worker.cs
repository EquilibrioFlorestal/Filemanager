using Domain.Adapters;
using Domain.Interfaces;
using Domain.Utils;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Spectre.Console;

namespace Peixe.SMDP.Worker
{
    public class Worker(IConfiguration configuration, IServiceProvider serviceProvider, IHostApplicationLifetime host) : BackgroundService
    {

        private readonly IConfiguration _configuration = configuration;
        private static readonly Queue<OrderProcessing> FilaRequisicoes = new();
        private static readonly object lockObj = new();
        private const string Extensao = ".zip";
        private const string FilenameOrders = "requests.json";
        private ushort _maxBatchTask = 800;

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

            lock (lockObj)
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
                ushort loadBatch = config.GetValue<ushort>("maxBatchTask");

                if (loadBatch != _maxBatchTask)
                {
                    AnsiConsole.MarkupLine($"[cyan]batch[/]: ajustado para {loadBatch} arquivos.");
                    _maxBatchTask = loadBatch;
                }

            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]Configuracao[/]: Falha ao ler Tags [[Peixe]] do arquivo de configuracoes.");
            }
        }

        static List<OrderProcessing>? LerRequisicoesJson()
        {
            string caminhoArquivoOrders = Path.Combine(Directory.GetCurrentDirectory(), FilenameOrders);
            if (!Path.Exists(caminhoArquivoOrders))
            {
                AnsiConsole.MarkupLine($"[red]Configuracao[/]: Arquivo de configuracao {FilenameOrders} ausente.");
                throw new FileNotFoundException();
            }

            string contentYaml = File.ReadAllText(caminhoArquivoOrders);
            List<OrderProcessing>? orders = JsonConvert.DeserializeObject<List<OrderProcessing>>(contentYaml);
            return orders;
        }

        void AdicionarTarefa(CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                List<OrderProcessing>? orders = LerRequisicoesJson();

                if (orders == null || orders.IsNullOrEmpty()) return;

                foreach (OrderProcessing tarefa in orders)
                {
                    if (tarefa.Validate() == false)
                    {
                        AnsiConsole.MarkupLine($"[white on red]Tarefa: {tarefa.Guid} não é válida.[/]");
                        continue;
                    }

                    if (cancellationToken.IsCancellationRequested) return;

                    FilaRequisicoes.Enqueue(tarefa);
                }

            }
        }

        void VerificarFilaTarefasAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (lockObj)
                {
                    if (FilaRequisicoes.Count <= 0)
                    {
                        host.StopApplication();
                        return;
                    }

                    OrderProcessing requisicao = FilaRequisicoes.Dequeue();

                    ProcessarTarefa(requisicao, cancellationToken).Wait();

                    AnsiConsole.MarkupLine($"[green]Tarefa[/]: Concluida {requisicao.Guid} [[Arquivos: {requisicao.FilesDownloaded}]]");
                    AnsiConsole.WriteLine();

                    int quantidadeSucesso = requisicao.OrderFiles.Where(x => x.IsSucessoProcessamento() == true).Count();

                    AnsiConsole.Write(new BarChart()
                        .Width(60)
                        .Label("Resumo transações").CenterLabel()
                        .AddItem("Sucesso", quantidadeSucesso, Color.Green)
                        .AddItem("Falha", requisicao.OrderFiles.Count - quantidadeSucesso, Color.Red));
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
                    ProgressTask task1 = ctx.AddTask("[green]Arquivo[/]: Processando arquivo");

                    task1.MaxValue = requisicao.OrderFiles.Count;

                    while (!ctx.IsFinished)
                    {
                        //requisicao.OrderFiles.ForEach(requisicaoArquivo =>
                        //{
                        //    ProcessarArquivo(requisicao, requisicaoArquivo).Wait();
                        //    task1.Increment(1);
                        //});

                        Parallel.ForEach(requisicao.OrderFiles, (requisicaoArquivo) =>
                        {
                            ProcessarArquivo(requisicao, requisicaoArquivo).Wait();
                            task1.Increment(1);
                        });
                    }
                });

            requisicao.FinishOrder();
            await requisicao.DefinirStatusOffline();
        }

        async Task ProcessarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
        {
            bool arquivoZipValido = requisicaoArquivo.ValidarArquivoZip();

            if (!arquivoZipValido)
            {
                AnsiConsole.MarkupLine($"[white on red]Arquivo: {requisicaoArquivo.NomeSemExtensao} esta corrompido !![/]");
            }

            requisicaoArquivo.Copy(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoDestino);
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
