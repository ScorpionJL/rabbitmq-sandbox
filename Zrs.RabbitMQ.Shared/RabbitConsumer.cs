using RabbitMQ.Client;

namespace Zrs.RabbitMQ.Shared;

public class RabbitConsumer<TMessage> where TMessage : MessageBase
{
    private readonly IMessageHandler<TMessage> _messageHandler;

    public RabbitConsumer(IMessageHandler<TMessage> messageHandler) : this(new ConnectionFactory(), messageHandler) { }
    public RabbitConsumer(ConnectionFactory connectionFactory, IMessageHandler<TMessage> messageHandler) : base(connectionFactory)
    {
        _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
    }
}
