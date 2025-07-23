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
