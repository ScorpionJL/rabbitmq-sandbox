using Zrs.RabbitMQ.Shared;

namespace Zrs.RabbitMQ.Contracts.Sandbox;

public record SandboxMessage : MessageBase;

public static class SandboxMessageBus
{
    const string EXCHANGE_NAME = "sandbox";
    const string QUEUE_NAME = EXCHANGE_NAME + "-queue";

    public static async Task<RabbitConnection> SetupTopology(RabbitConnection con)
    {
        await con.SetupExchangeAsync(new ExchangeOptions(EXCHANGE_NAME, ZrsExchangeType.Direct));
        await con.SetupQueueAsync(new QueueOptions(QUEUE_NAME, EXCHANGE_NAME));
        return con;
    }

    public static async Task<RabbitPublisher<SandboxMessage>> CreatePublisher(
        RabbitConnection connection,
        string routingKey = "",
        CancellationToken cancellationToken = default)
    {
        await SetupTopology(connection);
        return await connection.CreatePublisher<SandboxMessage>(EXCHANGE_NAME, routingKey, cancellationToken);
    }

    public static async Task<RabbitConsumer<SandboxMessage>> CreateConsumer(
        RabbitConnection connection,
        RabbitMessageHandler<SandboxMessage> messageHandler,
        string routingKey = "",
        CancellationToken cancellationToken = default)
    {
        await SetupTopology(connection);
        return await connection.CreateConsumer(messageHandler, QUEUE_NAME, routingKey, cancellationToken);
    }
}