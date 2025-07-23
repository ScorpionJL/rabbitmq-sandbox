namespace Zrs.RabbitMQ.Shared;

public interface IMessageHandler<T> where T : MessageBase
{
    Task HandleMessage(T message);
}

public class MessageHandler : IMessageHandler<MessageBase>
{
    public async Task HandleMessage(MessageBase message)
    {
        // Implement the logic to handle the message here
        // For example, you could log the message or process it in some way
        await Task.CompletedTask;
    }
}
