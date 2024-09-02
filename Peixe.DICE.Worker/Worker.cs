using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Models;
using Domain.Utils;
using MediatR;
using Newtonsoft.Json;
using Spectre.Console;

namespace Peixe.DICE.Worker;

public class Worker(IConfiguration configuration, IMediator mediator, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IMediator _mediator = mediator;
    private readonly IConfiguration _configuration = configuration;
    private static readonly Queue<OrderProcessing?> FilaRequisicoes = new Queue<OrderProcessing?>();
    private static readonly Object LockObj = new Object();
    private const Boolean EncerrarPrograma = false;
    private const String Extensao = ".zip";
    private const String FilenameOrders = "requests.json";
    private UInt16 _delaySecondsEachRequest = 10;
    private UInt16 _maxBatchTask = 20;
    private UInt16 _delayHoursEachBackgroundTagOffline = 8;
    private const Boolean DebugMode = false;
    private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(100);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        lock (LockObj) { AnsiConsole.MarkupLine($"[red]debug[/]: {DebugMode}."); }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!OnedriveUtils.CheckProcessOnedrive())
            {
                Exception ex = new Exception("No OneDrive process was found.");
                await _mediator.Publish(new RequiredProcessNotFoundNotification("OneDrive"), cancellationToken);
                AnsiConsole.WriteException(ex);
                return;
            }

            Task verificarFilaTask = Task.Run(() => VerificarFilaTarefasAsync(cancellationToken), cancellationToken);
            Task verificaArquivoBaixado = Task.Run(() => VerificaTodosArquivosBaixados(cancellationToken), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                lock (LockObj)
                {
                    CarregarConfiguracoesJson();

                    if (FilaRequisicoes.Count == 0)
                        AdicionarTarefa(cancellationToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(_delaySecondsEachRequest), cancellationToken);
            }

            await verificarFilaTask;
            await verificaArquivoBaixado;
        }
    }

    private async Task VerificaTodosArquivosBaixados(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || !EncerrarPrograma)
        {
            List<String> arquivos = OnedriveUtils.GetDownloadedFiles(OnedriveUtils.CaminhoOnedrive, "DICE", ".zip");
            Parallel.ForEach(arquivos, OnedriveUtils.SetOffline);
            await _mediator.Publish(new BackgroundTagOfflineExecutionNotification(arquivos.Count), cancellationToken);
            await Task.Delay(TimeSpan.FromHours(_delayHoursEachBackgroundTagOffline), cancellationToken);
        }
    }

    private void CarregarConfiguracoesJson()
    {
        try
        {
            IConfigurationSection config = _configuration.GetSection("Peixe");
            UInt16 loadDelay = config.GetValue<UInt16>("delaySecondsTask");
            UInt16 loadBatch = config.GetValue<UInt16>("maxBatchTask");
            UInt16 loadDelayBackgroundTagOffline = config.GetValue<UInt16>("delayHoursBackgroundTagOffline");

            if (loadDelayBackgroundTagOffline != _delayHoursEachBackgroundTagOffline)
            {
                AnsiConsole.MarkupLine($"[cyan]delay[/]: tarefa em segundo plano ajustada para {loadDelayBackgroundTagOffline} horas.");
                _delayHoursEachBackgroundTagOffline = loadDelayBackgroundTagOffline;
                _mediator.Publish(new AjusteDelayTagOfflineNotification(loadDelayBackgroundTagOffline));
            }

            if (loadDelay != _delaySecondsEachRequest)
            {
                AnsiConsole.MarkupLine($"[cyan]delay[/]: ajustado para {loadDelay} segundos.");
                _delaySecondsEachRequest = loadDelay;
                _mediator.Publish(new AjusteDelayNotification(loadDelay));
            }

            if (loadBatch != _maxBatchTask)
            {
                AnsiConsole.MarkupLine($"[cyan]batch[/]: ajustado para {loadBatch} arquivos.");
                _maxBatchTask = loadBatch;
                _mediator.Publish(new AjusteBatchNotification(loadBatch));
            }
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]configuracao[/]: Falha ao ler Tag [[Peixe.delaySeconds]] do arquivo de configuracoes.");
            _mediator.Publish(new FalhaTagArquivoConfiguracaoNotification("Peixe.delaySeconds", e.Message));
        }
    }

    List<OrderProcessing>? LerRequisicoesJson()
    {
        String caminhoArquivoOrders = Path.Combine(Directory.GetCurrentDirectory(), FilenameOrders);
        if (!Path.Exists(caminhoArquivoOrders))
        {
            _mediator.Publish(new ArquivoConfiguracaoAusenteNotification(FilenameOrders));
            AnsiConsole.MarkupLine($"[red]configuracao[/]: Arquivo de configuracao {FilenameOrders} ausente.");
            throw new FileNotFoundException();
        }

        String contentYaml = File.ReadAllText(caminhoArquivoOrders);
        List<OrderProcessing>? orders = JsonConvert.DeserializeObject<List<OrderProcessing>>(contentYaml);
        return orders;
    }

    void AdicionarTarefa(CancellationToken cancellationToken)
    {
        lock (LockObj)
        {
            List<OrderProcessing>? orders = LerRequisicoesJson();

            if (orders == null) return;

            foreach (OrderProcessing tarefa in orders)
            {

                if (DebugMode == true && tarefa.IdEmpresa != 1)
                    continue;

                if (!tarefa.Validate())
                {
                    AnsiConsole.MarkupLine($"[white on red]tarefa: {tarefa.Guid} nao e valida.[/]");
                    _mediator.Publish(new TarefaInvalidaNotification(tarefa), cancellationToken);
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                FilaRequisicoes.Enqueue(tarefa);
            }
        }
    }

    void VerificarFilaTarefasAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || !EncerrarPrograma)
        {
            lock (LockObj)
            {
                if (FilaRequisicoes.Count <= 0) continue;

                OrderProcessing? requisicao = FilaRequisicoes.Dequeue();

                if (requisicao == null) continue;

                // AnsiConsole.MarkupLine($"Tarefa: Iniciando {requisicao.Guid}");
                // _mediator.Publish(new TarefaIniciadaNotification(requisicao), cancellationToken);

                 ProcessarTarefa(requisicao, cancellationToken).Wait(cancellationToken);

                if (requisicao.FilesDownloaded > 0)
                {
                    AnsiConsole.WriteLine($"Tarefa: Concluida {requisicao.Guid} [Arquivos: {requisicao.FilesDownloaded}, Objects: {requisicao.ObjectsDownload}]");
                    _mediator.Publish(new TarefaConcluidaNotification(requisicao), cancellationToken);
                }

                //_mediator.Publish(new TarefaConcluidaVaziaNotification(requisicao), cancellationToken);
            }
        }
    }

    async Task ProcessarTarefa(OrderProcessing requisicao, CancellationToken cancellationToken)
    {
        requisicao.ProcurarArquivos(Extensao, cancellationToken, _maxBatchTask);

        cancellationToken.ThrowIfCancellationRequested();

        if (requisicao.OrderFiles.Count > 0)
        {
            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _maxBatchTask };
            Parallel.ForEach(requisicao.OrderFiles, parallelOptions, (requisicaoArquivo) =>
            {
                ProcessarArquivo(requisicao, requisicaoArquivo).Wait();
            });

            //foreach (OrderFileProcessing requisicaoArquivo in requisicao.OrderFiles)
            //{
            //    await ProcessarArquivo(requisicao, requisicaoArquivo);
            //}
        }

        requisicao.FinishOrder();
        await requisicao.DefinirStatusOffline();
    }

    async Task ProcessarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
    {
        Boolean arquivoZipValido = requisicaoArquivo.ValidarArquivoZip();

        if (!arquivoZipValido)
        {
            AnsiConsole.MarkupLine($"[white on red]arquivo: {requisicaoArquivo.NomeSemExtensao} esta corrompido !![/]");
            await _mediator.Publish(new ArquivoCorrompidoNotification(requisicaoArquivo));
        }

        requisicaoArquivo.Copy(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoDestino);
        requisicaoArquivo.Move(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoBackup, fake: false);

        Boolean validadeProcessamento = await requisicaoArquivo.ValidarProcessamento();

        requisicao.FilesDownloaded += 1;

        if (!validadeProcessamento)
        {
            lock (LockObj)
            {
                AnsiConsole.MarkupLine($"[red]transferencia[/]: {requisicaoArquivo.NomeSemExtensao}");
                _mediator.Publish(new TransferenciaInvalidaNotification(requisicaoArquivo));
            }
            return;
        }

        await _mediator.Publish(new ArquivoProcessandoNotification(requisicaoArquivo, validadeProcessamento));

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IArquivoService service = scope.ServiceProvider.GetRequiredService<IArquivoService>();

            Boolean jaCadastrado = await service.VerificarCadastrado(requisicaoArquivo.Nome, requisicao.Modulo, requisicao.IdEmpresa);

            if (!jaCadastrado)
            {
                (Boolean resposta, String mensagem) = await service.CadastrarArquivo(requisicao, requisicaoArquivo);
                if (!resposta) await _mediator.Publish(new ErroAdicionarArquivoNotification(requisicaoArquivo, mensagem));
            }
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ITalhaoService service = scope.ServiceProvider.GetRequiredService<ITalhaoService>();
            IProgramacaoService programacaoService = scope.ServiceProvider.GetRequiredService<IProgramacaoService>();

            List<Task> tasks = requisicaoArquivo.OrderTalhoes.Select(async orderTalhao =>
            {
                await Semaphore.WaitAsync();
                try
                {
                    (Programacao? programacao, String mensagemAtualizar) = await programacaoService.Atualizar(orderTalhao);
                    if (programacao == null)
                    {
                        await _mediator.Publish(new ErroAtualizarTalhaoNaProgramacaoNotification(orderTalhao, mensagemAtualizar));
                    }

                    Boolean jaCadastrado = await service.VerificarCadastrado(orderTalhao.NomeArquivo, orderTalhao.ProgramacaoRetornoGuid);

                    if (!jaCadastrado)
                    {
                        (Boolean resposta, String mensagemCadastrar) = await service.CadastrarTalhao(orderTalhao);
                        if (!resposta)
                        {
                            await _mediator.Publish(new ErroAdicionarTalhaoNotification(orderTalhao, mensagemCadastrar));
                        }
                    }
                }
                finally
                {
                    requisicao.ObjectsDownload += 1;
                    Semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IImagemService service = scope.ServiceProvider.GetRequiredService<IImagemService>();

            List<Task> tasks = requisicaoArquivo.OrderImagens.Select(async orderImagem =>
            {
                await Semaphore.WaitAsync();
                try
                {
                    Boolean jaCadastrado = await service.VerificarCadastrado(orderImagem.NomeImagem, orderImagem.ProgramacaoRetornoGuid);

                    if (!jaCadastrado)
                    {
                        (Boolean resposta, String mensagem) = await service.CadastrarImagem(orderImagem);
                        if (!resposta) await _mediator.Publish(new ErroAdicionarImagemNotification(orderImagem, mensagem));
                    }
                }
                finally
                {
                    Semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }
        //await Task.Run(() => Console.WriteLine($"Arquivo processado: {requisicaoArquivo.NomeSemExtensao}"));
    }

    private static IEnumerable<List<T>> Partition<T>(List<T> source, Int32 size)
    {
        for (Int32 i = 0; i < source.Count; i += size)
        {
            yield return source.GetRange(i, Math.Min(size, source.Count - i));
        }
    }
}

