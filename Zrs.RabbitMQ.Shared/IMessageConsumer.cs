namespace Zrs.RabbitMQ.Shared;

public interface IMessageConsumer<T> where T : MessageBase
{
    static abstract Task<T> CreateConsumerAsync(
        Func<T, Task> messageHandler,
        CancellationToken cancellationToken);

    Task StartConsumer(CancellationToken cancellationToken);

    Task StopConsumer(CancellationToken cancellationToken);
}
