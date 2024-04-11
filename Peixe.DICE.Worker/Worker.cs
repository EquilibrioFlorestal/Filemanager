using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Utils;
using MediatR;
using Newtonsoft.Json;
using Spectre.Console;

namespace Peixe.Worker;

public class Worker(IConfiguration configuration, IMediator mediator, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IMediator _mediator = mediator;
    private readonly IConfiguration _configuration = configuration;
    private static readonly Queue<OrderProcessing?> FilaRequisicoes = new Queue<OrderProcessing?>();
    private static readonly object LockObj = new object();
    private const bool EncerrarPrograma = false;
    private const string Extensao = ".zip";
    private const string FilenameOrders = "requests.json";
    private ushort _delaySecondsEachRequest = 10;
    private ushort _maxBatchTask = 20;
    private ushort _delayHoursEachBackgroundTagOffline = 8;
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
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
                    {
                        AdicionarTarefa(cancellationToken);
                    }
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
            List<string> arquivos = OnedriveUtils.GetDownloadedFiles(OnedriveUtils.CaminhoOnedrive, "DICE", ".zip");
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
            ushort loadDelay = config.GetValue<ushort>("delaySecondsTask");
            ushort loadBatch = config.GetValue<ushort>("maxBatchTask");
            ushort loadDelayBackgroundTagOffline = config.GetValue<ushort>("delayHoursBackgroundTagOffline");

            if (loadDelayBackgroundTagOffline != _delayHoursEachBackgroundTagOffline)
            {
                AnsiConsole.MarkupLine($"[cyan]Delay[/]: tarefa em segundo plano ajustada para {loadDelayBackgroundTagOffline} horas.");
                _delayHoursEachBackgroundTagOffline = loadDelayBackgroundTagOffline;
                _mediator.Publish(new AjusteDelayTagOfflineNotification(loadDelayBackgroundTagOffline));
            }
            
            if (loadDelay != _delaySecondsEachRequest)
            {
                AnsiConsole.MarkupLine($"[cyan]Delay[/]: ajustado para {loadDelay} segundos.");
                _delaySecondsEachRequest = loadDelay;
                _mediator.Publish(new AjusteDelayNotification(loadDelay));
            }

            if (loadBatch != _maxBatchTask)
            {
                AnsiConsole.MarkupLine($"[cyan]Batch[/]: ajustado para {loadBatch} arquivos.");
                _maxBatchTask = loadBatch;
                _mediator.Publish(new AjusteBatchNotification(loadBatch));
            }
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Configuracao[/]: Falha ao ler Tag [[Peixe.delaySeconds]] do arquivo de configuracoes.");
            _mediator.Publish(new FalhaTagArquivoConfiguracaoNotification("Peixe.delaySeconds", e.Message));
        }
    }
    
    List<OrderProcessing>? LerRequisicoesJson()
    {
        string caminhoArquivoOrders = Path.Combine(Directory.GetCurrentDirectory(), FilenameOrders);
        if (!Path.Exists(caminhoArquivoOrders))
        {
            _mediator.Publish(new ArquivoConfiguracaoAusenteNotification(FilenameOrders));
            AnsiConsole.MarkupLine($"[red]Configuracao[/]: Arquivo de configuracao {FilenameOrders} ausente.");
            throw new FileNotFoundException();
        }
        
        string contentYaml = File.ReadAllText(caminhoArquivoOrders);
        List<OrderProcessing>? orders = JsonConvert.DeserializeObject<List<OrderProcessing>>(contentYaml);
        return orders;
    }

    void AdicionarTarefa(CancellationToken cancellationToken)
    {
        lock (LockObj)
        {
            List<OrderProcessing>? orders = LerRequisicoesJson();
            
            orders?.ForEach(tarefa =>
            {
                if (!tarefa.Validate())
                {
                    AnsiConsole.MarkupLine($"[white on red]Tarefa: {tarefa.Guid} nao e valida.[/]");
                    _mediator.Publish(new TarefaInvalidaNotification(tarefa), cancellationToken);
                    return;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                FilaRequisicoes.Enqueue(tarefa);
            });
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
                
                Task tarefas = ProcessarTarefa(requisicao, cancellationToken);

                if (requisicao.FilesDownloaded > 0)
                    AnsiConsole.WriteLine($"Tarefa: Concluida {requisicao.Guid} [Arquivos: {requisicao.FilesDownloaded}]");
                _mediator.Publish(new TarefaConcluidaNotification(requisicao), cancellationToken);
                //_mediator.Publish(new TarefaConcluidaVaziaNotification(requisicao), cancellationToken);

            }
        }
    }
    
    async Task ProcessarTarefa(OrderProcessing requisicao, CancellationToken cancellationToken)
    {
        requisicao.OrderFiles = ProcurarArquivos(requisicao, Extensao, cancellationToken, _maxBatchTask);
        
        cancellationToken.ThrowIfCancellationRequested();
        foreach (OrderFileProcessing requisicaoArquivo in requisicao.OrderFiles)
        {
            await ProcessarArquivo(requisicao, requisicaoArquivo);
        }

        requisicao.FinishOrder();
        await requisicao.DefinirStatusOffline();
    }

    async Task ProcessarArquivo(OrderProcessing requisicao, OrderFileProcessing requisicaoArquivo)
    {
        bool arquivoZipValido = requisicaoArquivo.ValidarArquivoZip();
        
        if (!arquivoZipValido)
        {
            AnsiConsole.MarkupLine($"[white on red]Arquivo: {requisicaoArquivo.NomeSemExtensao} esta corrompido !![/]");
            await _mediator.Publish(new ArquivoCorrompidoNotification(requisicaoArquivo));
        }

        requisicaoArquivo.Copy(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoDestino);
        requisicaoArquivo.Move(requisicaoArquivo.CaminhoOrigem, requisicaoArquivo.CaminhoBackup);
        
        bool validadeProcessamento = await requisicaoArquivo.ValidarProcessamento();

        requisicao.FilesDownloaded += 1;
        
        if (!validadeProcessamento)
        {
            AnsiConsole.MarkupLine($"[red]Transferencia[/]: {requisicaoArquivo.NomeSemExtensao}");
            await _mediator.Publish(new TransferenciaInvalidaNotification(requisicaoArquivo));
            return;
        }

        await _mediator.Publish(new ArquivoProcessandoNotification(requisicaoArquivo, validadeProcessamento));
        
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IArquivoService service = scope.ServiceProvider.GetRequiredService<IArquivoService>();
            
            bool jaCadastrado = await service.VerificarCadastrado(requisicaoArquivo.Nome, requisicao.Modulo, requisicao.IdEmpresa);

            if (!jaCadastrado)
            {
                var (resposta, mensagem) = await service.CadastrarArquivo(requisicao, requisicaoArquivo);
                if (!resposta) await _mediator.Publish(new ErroAdicionarArquivoNotification(requisicaoArquivo, mensagem));
            }
        }
        
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ITalhaoService service = scope.ServiceProvider.GetRequiredService<ITalhaoService>();
            
            foreach (OrderTalhaoProcessing orderTalhao in requisicaoArquivo.OrderTalhoes)
            {
                bool jaCadastrado = await service.VerificarCadastrado(orderTalhao.NomeArquivo, orderTalhao.ProgramacaoRetornoGuid);

                if (jaCadastrado) continue;

                var (resposta, mensagem) = await service.CadastrarTalhao(orderTalhao);
                if (!resposta) await _mediator.Publish(new ErroAdicionarTalhaoNotification(orderTalhao, mensagem));
            }
        }
        
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IImagemService service = scope.ServiceProvider.GetRequiredService<IImagemService>();
            
            foreach (OrderImageProcessing orderImagem in requisicaoArquivo.OrderImagens)
            {
                bool jaCadastrado = await service.VerificarCadastrado(orderImagem.NomeImagem, orderImagem.ProgramacaoRetornoGuid);

                if (jaCadastrado) continue;

                var (resposta, mensagem) = await service.CadastrarImagem(orderImagem);
                if (!resposta) await _mediator.Publish(new ErroAdicionarImagemNotification(orderImagem, mensagem));
            }
        }
        
    }

    private List<OrderFileProcessing> ProcurarArquivos(OrderProcessing requisicao, string extensao, CancellationToken cancellationToken, ushort? maxBatch = 20)
    {
        cancellationToken.ThrowIfCancellationRequested();

        extensao = extensao.Replace(".", string.Empty);

        List<string> localArquivos = requisicao.PastaOrigem.SelectMany(pasta =>
            Directory.GetFiles(pasta, $"{requisicao.Modulo}_*.{extensao}", SearchOption.AllDirectories).OrderBy(f => new FileInfo(f).Length)).Take(_maxBatchTask).ToList();

        List<OrderFileProcessing> listaArquivos = [];
        listaArquivos.AddRange(localArquivos.Select(arquivo => new OrderFileProcessing(arquivo, requisicao.PastaDestino, requisicao.PastaBackup, requisicao.Modulo, requisicao.IdEmpresa)));

        return listaArquivos;
    }
}