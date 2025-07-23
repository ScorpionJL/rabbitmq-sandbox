namespace Zrs.RabbitMQ.Shared;

public interface IMessagePublisher<T> where T : MessageBase
{
    static abstract Task<T> CreatePublisherAsync(CancellationToken cancellationToken);

    ValueTask PublishMessage(SandboxMessage message, CancellationToken cancellationToken);
}

