namespace Zrs.RabbitMQ.Shared;

public static class RabbitTopology
{
    public static async Task<RabbitConnection> SetupExchangeAsync(this RabbitConnection connection, ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        using var channel = await connection.CreateChannel(cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchangeOptions.ExchangeName,
            exchangeOptions.ExchangeType,
            durable: true, autoDelete: false, arguments: null,
            cancellationToken: cancellationToken);
        return connection;
    }

    public static async Task<RabbitConnection> SetupQueueAsync(this RabbitConnection connection, QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        using var channel = await connection.CreateChannel(cancellationToken);
        await channel.QueueDeclareAsync(
            queueOptions.QueueName,
            durable: true, exclusive: false, autoDelete: false, arguments: null,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queueOptions.QueueName,
            queueOptions.ExchangeName,
            queueOptions.RoutingKey ?? string.Empty,
            cancellationToken: cancellationToken);
        return connection;
    }
}
