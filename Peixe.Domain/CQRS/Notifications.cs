using Domain.Adapters;
using MediatR;
using System.ComponentModel.Design;

namespace Domain.CQRS;

public class RequiredProcessNotFoundNotification(String processName) : INotification
{
    public String nomeProcesso { get; set; } = processName;
}
public class BackgroundTagOfflineExecutionNotification(Int32 quantidade) : INotification
{
    public Int32 quantidade { get; set; } = quantidade;
}

public class AjusteDelayTagOfflineNotification(UInt16 newDelay) : INotification
{
    public UInt16 delay { get; set; } = newDelay;
}
public class AjusteDelayNotification(UInt16 newDelayValue) : INotification
{
    public UInt16 delay { get; set; } = newDelayValue;
}

public class AjusteBatchNotification(UInt16 newBatch) : INotification
{
    public UInt16 batch { get; set; } = newBatch;
}

public class FalhaTagArquivoConfiguracaoNotification(String tagName, String mensagem) : INotification
{
    public String tag { get; set; } = tagName;
    public String mensagem { get; set; } = mensagem;
}

public class ArquivoConfiguracaoAusenteNotification(String filename) : INotification
{
    public String filename { get; set; } = filename;
}

public class ArquivoCorrompidoNotification(OrderFileProcessing order) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
}

public class ArquivoProcessandoNotification(OrderFileProcessing order, Boolean transferido) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
    public Boolean transferido { get; set; } = transferido;
}

public class TarefaInvalidaNotification(OrderProcessing order) : INotification
{
    public OrderProcessing order { get; set; } = order;
}

public class TarefaIniciadaNotification(OrderProcessing order) : INotification
{
    public OrderProcessing order { get; set; } = order;
}

public class TarefaConcluidaNotification(OrderProcessing order) : INotification
{
    public OrderProcessing order { get; set; } = order;
}

public class TarefaConcluidaVaziaNotification(OrderProcessing order) : INotification
{
    public OrderProcessing order { get; set; } = order;
}

public class TarefaNaoConcluidaNotification(OrderProcessing order, Exception ex) : INotification
{
    public OrderProcessing Order { get; set; } = order;
    public Exception exception { get; set; } = ex;
}

public class TransferenciaInvalidaNotification(OrderFileProcessing order) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
}

public class ErroAdicionarTalhaoNotification(OrderTalhaoProcessing order, String mensagem) : INotification
{
    public OrderTalhaoProcessing order { get; set; } = order;
    public String mensagem { get; set; } = mensagem;
}

public class ArquivoProcessadoNotification(OrderFileProcessing order): INotification
{
    public OrderFileProcessing order { get; set; } = order;
}

public class ErroAdicionarImagemNotification(OrderImageProcessing order, String mensagem) : INotification
{
    public OrderImageProcessing order { get; set; } = order;
    public String mensagem { get; set; } = mensagem;
}

public class ErroAdicionarArquivoNotification(OrderFileProcessing order, String mensagem) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
    public String mensagem { get; set; } = mensagem;
}

public class ErroAtualizarTalhaoNaProgramacaoNotification(OrderTalhaoProcessing order, String mensagem) : INotification
{
    public OrderTalhaoProcessing order { get; set; } = order;
    public String mensagem { get; set; } = mensagem;
}