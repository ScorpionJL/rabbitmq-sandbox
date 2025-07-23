using RabbitMQ.Client;

namespace Zrs.RabbitMQ.Shared;

public class SandboxMessageClient
{
    public const string EXCHANGE_NAME = "sandbox";
    public const string QUEUE_NAME = EXCHANGE_NAME + "-queue";
    protected readonly RabbitService _rabbitClient;

    public SandboxMessageClient()
    {
        _rabbitClient = new RabbitService();
    }

    public static async Task<SandboxMessageClient> Create(CancellationToken cancellationToken = default)
    {
        var client = new RabbitService();
        await client.TryConnect(cancellationToken);
        await client.SetupExchangeAsync(new ExchangeOptions(EXCHANGE_NAME, ZrsExchangeType.Direct), cancellationToken);
        await client.SetupQueueAsync(new QueueOptions(QUEUE_NAME, EXCHANGE_NAME), cancellationToken);
        return client;
    }
}
