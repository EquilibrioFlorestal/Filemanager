using Domain.Adapters;
using MediatR;

namespace Domain.CQRS;

public class AjusteDelayNotification(ushort newDelayValue) : INotification
{
    public ushort delay { get; set; } = newDelayValue;
}

public class FalhaTagArquivoConfiguracaoNotification(string tagName, string mensagem) : INotification
{
    public string tag { get; set; } = tagName;
    public string mensagem { get; set; } = mensagem;
}

public class ArquivoConfiguracaoAusenteNotification(string filename) : INotification
{
    public string filename { get; set; } = filename;
}
    
public class ArquivoCorrompidoNotification(OrderFileProcessing order) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
}

public class ArquivoProcessandoNotification(OrderFileProcessing order, bool transferido) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
    public bool transferido { get; set; } = transferido;
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

public class TransferenciaInvalidaNotification(OrderFileProcessing order) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
}

public class ErroAdicionarTalhaoNotification(OrderTalhaoProcessing order, string mensagem) : INotification
{
    public OrderTalhaoProcessing order { get; set; }
    public string mensagem { get; set; } = mensagem;
}

public class ErroAdicionarImagemNotification(OrderImageProcessing order, string mensagem) : INotification
{
    public OrderImageProcessing order { get; set; } = order;
    public string mensagem { get; set; } = mensagem;
}

public class ErroAdicionarArquivoNotification(OrderFileProcessing order, string mensagem) : INotification
{
    public OrderFileProcessing order { get; set; } = order;
    public string mensagem { get; set; } = mensagem;
    
}