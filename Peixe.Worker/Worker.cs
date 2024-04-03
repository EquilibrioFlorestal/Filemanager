using Domain.Adapters;
using Domain.CQRS;
using Domain.Interfaces;
using Domain.Utils;
using MediatR;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Newtonsoft.Json;
using Spectre.Console;

namespace Peixe.Worker;

public class Worker(IConfiguration configuration, IMediator mediator, ILogger<Worker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<Worker> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private static readonly Queue<OrderProcessing?> FilaRequisicoes = new Queue<OrderProcessing?>();
    private static readonly object LockObj = new object();
    private const bool EncerrarPrograma = false;
    private const string Extensao = ".zip";
    private const string FilenameOrders = "requests.json";
    private ushort _delaySecondsEachRequest = 10;
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {

        lock (LockObj)
        {
            IHost server = serviceProvider.GetRequiredService<IHost>();
            
        }
        
        while (!cancellationToken.IsCancellationRequested)
        {
            Task verificarFilaTask = Task.Run(() => VerificarFilaTarefasAsync(cancellationToken), cancellationToken);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (LockObj)
                {
                    CarregarConfiguracoesJson();
                    
                    if (FilaRequisicoes.Count == 0)
                    {
                         //Console.Clear();
                        AdicionarTarefa(cancellationToken);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(_delaySecondsEachRequest), cancellationToken);
            }

            await verificarFilaTask;
        }
    }

    private void CarregarConfiguracoesJson()
    {
        try
        {
            IConfigurationSection config = _configuration.GetSection("Peixe");
            ushort loadDelay = config.GetValue<ushort>("delaySeconds");

            if (loadDelay == _delaySecondsEachRequest) return;
            
            AnsiConsole.MarkupLine($"[cyan]Delay[/]: ajustado para {loadDelay} segundos.");
            _delaySecondsEachRequest = loadDelay;
            _mediator.Publish(new AjusteDelayNotification(loadDelay));
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Configuracao[/]: Falha ao ler Tag [[Peixe.delaySeconds]] do arquivo de configuracoes.");
            _mediator.Publish(new FalhaTagArquivoConfiguracaoNotification("Peixe.delaySeconds", e.Message));
            return;
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

                AnsiConsole.WriteLine($"Tarefa: Concluida {requisicao.Guid} [Arquivos: {requisicao.FilesDownloaded}]");
                _mediator.Publish(new TarefaConcluidaNotification(requisicao), cancellationToken);
                //_mediator.Publish(new TarefaConcluidaVaziaNotification(requisicao), cancellationToken);

            }
        }
    }
    
    async Task ProcessarTarefa(OrderProcessing requisicao, CancellationToken cancellationToken)
    {
        requisicao.OrderFiles = DirectoryUtils.ProcurarArquivos(requisicao, Extensao, cancellationToken);
        
        cancellationToken.ThrowIfCancellationRequested();
        foreach (OrderFileProcessing requisicaoArquivo in requisicao.OrderFiles)
        {
            await ProcessarArquivo(requisicao, requisicaoArquivo);
        }

        requisicao.FinishOrder();
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
        
            if (!jaCadastrado) await service.CadastrarArquivo(requisicao, requisicaoArquivo);
        }
        
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ITalhaoService service = scope.ServiceProvider.GetRequiredService<ITalhaoService>();
            
            foreach (OrderTalhaoProcessing orderTalhao in requisicaoArquivo.OrderTalhoes)
            {
                bool jaCadastrado = await service.VerificarCadastrado(orderTalhao.NomeArquivo, orderTalhao.ProgramacaoRetornoGuid);
        
                if (!jaCadastrado) 
                    await service.CadastrarTalhao(orderTalhao);
            }
        }
        
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IImagemService service = scope.ServiceProvider.GetRequiredService<IImagemService>();
            
            foreach (OrderImageProcessing orderImagem in requisicaoArquivo.OrderImagens)
            {
                bool jaCadastrado = await service.VerificarCadastrado(orderImagem.NomeImagem, orderImagem.ProgramacaoRetornoGuid);
        
                if (! jaCadastrado)
                    await service.CadastrarImagem(orderImagem);
            }
        }

        requisicao.FilesDownloaded += 1;
    }
        
}