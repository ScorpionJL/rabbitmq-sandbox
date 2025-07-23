using RabbitMQ.Client;

namespace Zrs.RabbitMQ.Shared;

public class SandboxPublisher : RabbitPublisher<SandboxMessage>
{
    public const string EXCHANGE_NAME = "sandbox";
    public const string QUEUE_NAME = EXCHANGE_NAME + "-queue";


    public SandboxPublisher() : base() { }
    public SandboxPublisher(ConnectionFactory connectionFactory) : base(connectionFactory) { }

    public Task PublishMessage(SandboxMessage message, CancellationToken cancellationToken = default) => PublishMessage(message, EXCHANGE_NAME, cancellationToken);
}
