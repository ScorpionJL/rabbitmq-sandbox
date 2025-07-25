using RabbitMQ.Client;

namespace Zrs.RabbitMQ.Shared;

public sealed record ExchangeOptions(string ExchangeName, ZrsExchangeType ExchangeType)
{
    public ExchangeOptions(string Name) : this(Name, ZrsExchangeType.Direct) { }
}

public sealed record ZrsExchangeType
{
    public static readonly ZrsExchangeType Direct = new(ExchangeType.Direct);
    public static readonly ZrsExchangeType Fanout = new(ExchangeType.Fanout);
    public static readonly ZrsExchangeType Topic = new(ExchangeType.Topic);
    public static readonly ZrsExchangeType Headers = new(ExchangeType.Headers);

    private readonly string _value;

    private ZrsExchangeType(string Value) => _value = Value;

    public static implicit operator string(ZrsExchangeType exchangeType) => exchangeType._value;
}

public sealed record QueueOptions(string QueueName, string ExchangeName, string? RoutingKey = null);


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
