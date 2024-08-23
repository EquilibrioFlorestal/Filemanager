using MediatR;
using Microsoft.Extensions.Logging;

namespace Domain.CQRS;

public class RequiredProcessHandler(ILogger<RequiredProcessHandler> logger) : INotificationHandler<RequiredProcessNotFoundNotification>
{
    private readonly ILogger<RequiredProcessHandler> _logger = logger;


    public Task Handle(RequiredProcessNotFoundNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogCritical($"Process: Processo requerido de nome {notification.nomeProcesso} não encontrado.");
        return Task.CompletedTask;
    }
}

public class BackgroundTaskHandler(ILogger<BackgroundTaskHandler> logger) : INotificationHandler<BackgroundTagOfflineExecutionNotification>, INotificationHandler<AjusteDelayTagOfflineNotification>
{
    private readonly ILogger<BackgroundTaskHandler> _logger = logger;

    public Task Handle(BackgroundTagOfflineExecutionNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Background: Executando atividade em segundo plano. [Arquivos baixados: {notification.quantidade}]");
        return Task.CompletedTask;
    }

    public Task Handle(AjusteDelayTagOfflineNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Delay: Delay entre tarefas em segundo plano ajustado para {notification.delay} horas.");
        return Task.CompletedTask;
    }
}

public class ArquivoConfiguracaoHandler(ILogger<ArquivoConfiguracaoHandler> logger) : INotificationHandler<ArquivoConfiguracaoAusenteNotification>, INotificationHandler<AjusteDelayNotification>, INotificationHandler<FalhaTagArquivoConfiguracaoNotification>, INotificationHandler<AjusteBatchNotification>
{
    private readonly ILogger<ArquivoConfiguracaoHandler> _logger = logger;

    public Task Handle(ArquivoConfiguracaoAusenteNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogCritical($"Configuracao: Arquivo de configuracao {notification.filename} ausente.");
        return Task.CompletedTask;
    }

    public Task Handle(AjusteDelayNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Delay: Delay entre tarefas ajustado para {notification.delay} segundos.");
        return Task.CompletedTask;
    }

    public Task Handle(FalhaTagArquivoConfiguracaoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogError($"Configuracao: Falha ao ler tag [{notification.tag.ToString()}] do arquivo de configuracao. Motivo: {notification.mensagem}");
        return Task.CompletedTask;
    }

    public Task Handle(AjusteBatchNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Batch: Batch ajustado para {notification.batch} arquivos.");
        return Task.CompletedTask;
    }
}

public class TalhaoHandler(ILogger<TalhaoHandler> logger) : INotificationHandler<ErroAdicionarTalhaoNotification>, INotificationHandler<ErroAtualizarTalhaoNaProgramacaoNotification>
{
    private readonly ILogger<TalhaoHandler> _logger = logger;

    public Task Handle(ErroAdicionarTalhaoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Talhao: {notification.order.ProgramacaoRetornoGuid} do arquivo {notification.order.NomeArquivo} não adicionado.");
        return Task.CompletedTask;
    }

    public Task Handle(ErroAtualizarTalhaoNaProgramacaoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Programação: {notification.order.ProgramacaoGuid} não foi possível atualizar na tabela Programação.");
        return Task.CompletedTask;
    }
}

public class ImagemHandler(ILogger<ImagemHandler> logger) : INotificationHandler<ErroAdicionarImagemNotification>
{
    private readonly ILogger<ImagemHandler> _logger = logger;

    public Task Handle(ErroAdicionarImagemNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Imagem: {notification.order.NomeImagem} não adicionada.");
        return Task.CompletedTask;
    }
}

public class ArquivoHandler(ILogger<ArquivoHandler> logger) : INotificationHandler<ArquivoCorrompidoNotification>, INotificationHandler<ErroAdicionarArquivoNotification>, INotificationHandler<ArquivoProcessandoNotification>, INotificationHandler<TransferenciaInvalidaNotification>
{
    private readonly ILogger<ArquivoHandler> _logger = logger;

    public Task Handle(ArquivoCorrompidoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Arquivo: {notification.order.NomeSemExtensao} esta corrompido.");
        return Task.CompletedTask;
    }

    public Task Handle(ErroAdicionarArquivoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Arquivo: {notification.order.NomeSemExtensao} não adicionado.");
        return Task.CompletedTask;
    }

    public Task Handle(ArquivoProcessandoNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Arquivo: {notification.order.NomeSemExtensao}, transferido: {notification.transferido}.");
        return Task.CompletedTask;
    }

    public Task Handle(TransferenciaInvalidaNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning($"Arquivo: Nao foi possivel validar a transferencia do arquivo {notification.order.NomeSemExtensao}.");
        return Task.CompletedTask;
    }
}

public class TarefaHandler(ILogger<TarefaHandler> logger) : INotificationHandler<TarefaInvalidaNotification>, INotificationHandler<TarefaConcluidaNotification>, INotificationHandler<TarefaIniciadaNotification>, INotificationHandler<TarefaConcluidaVaziaNotification>
{
    private readonly ILogger<TarefaHandler> _logger = logger;

    public Task Handle(TarefaInvalidaNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogCritical($"Tarefa: {notification.order.Guid} nao e valida.");
        return Task.CompletedTask;
    }

    public Task Handle(TarefaConcluidaNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Tarefa: Concluida {notification.order.Guid} [Files: {notification.order.FilesDownloaded}] [Elapsed: {notification.order.ElapsedTime}]");
        return Task.CompletedTask;
    }

    public Task Handle(TarefaIniciadaNotification notification, CancellationToken cancellationToken)
    {
        // _logger.LogInformation($"Tarefa: Iniciando {notification.order.Guid}");
        return Task.CompletedTask;
    }

    public Task Handle(TarefaConcluidaVaziaNotification notification, CancellationToken cancellationToken)
    {
        // _logger.LogInformation($"Tarefa: Concluida {notification.order.Guid} [{notification.order.ElapsedTime}]");
        return Task.CompletedTask;
    }
}