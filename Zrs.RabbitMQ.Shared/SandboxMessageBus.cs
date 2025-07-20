using System.Threading;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zrs.RabbitMQ.Shared.Extensions;

namespace Zrs.RabbitMQ.Shared;

public sealed class SandboxMessageBus : IDisposable, IAsyncDisposable
{
    public const string EXCHANGE_NAME = "sandbox";
    public const string QUEUE_NAME = EXCHANGE_NAME + "-queue";


    // factory methods
    public static Task<SandboxMessageBus> CreatePublisherAsync(CancellationToken cancellationToken = default) => CreateBusAsync(cancellationToken);

    public static async Task<SandboxMessageBus> CreateConsumerAsync(
        Func<SandboxMessage, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageHandler);

        var messageBus = await CreateBusAsync(cancellationToken);
        messageBus.SetupConsumer(messageHandler);
        return messageBus;
    }

    private static async Task<SandboxMessageBus> CreateBusAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory();
        var connection = await factory.CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var messageBus = new SandboxMessageBus(connection, channel);
        await messageBus.SetupExchangeAsync(cancellationToken);
        await messageBus.SetupQueueAsync(cancellationToken);

        return messageBus;
    }


    // static methods for publishing messages
    public static ValueTask PublishMessage(IChannel channel, SandboxMessage message, CancellationToken cancellationToken = default)
    {
        return channel.PublishMessage(EXCHANGE_NAME, message, cancellationToken);
    }


    // fields    
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private CancellationToken _consumerToken;


    // constructor
    private SandboxMessageBus(IConnection connection, IChannel channel) => (_connection, _channel) = (connection, channel);

    private async Task<SandboxMessageBus> SetupExchangeAsync(CancellationToken cancellationToken = default)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        return this;
    }

    private async Task<SandboxMessageBus> SetupQueueAsync(CancellationToken cancellationToken = default)
    {
        await _channel.QueueDeclareAsync(
            queue: QUEUE_NAME,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QUEUE_NAME,
            exchange: EXCHANGE_NAME,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
        return this;
    }

    private SandboxMessageBus SetupConsumer(Func<SandboxMessage, Task> messageHandler)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, e) =>
        {
            var message = e.GetMessage<SandboxMessage>();
            if (message is not null) { await messageHandler(message); }
            await _channel.BasicAckAsync(deliveryTag: e.DeliveryTag, multiple: false);
        };
        _consumer = consumer;
        return this;
    }


    // publish messages
    public ValueTask PublishMessage(SandboxMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.PublishMessage(EXCHANGE_NAME, message, cancellationToken);
    }

    
    // consume messages
    public Task StartConsumer(CancellationToken cancellationToken = default)
    {
        if (_consumer == null)
            throw new InvalidOperationException("Consumer not initialized. Use CreateConsumerAsync with a message handler.");
        return _channel.BasicConsumeAsync(QUEUE_NAME, autoAck: false, _consumer, cancellationToken);
    }

    public Task StopConsumer(CancellationToken cancellationToken = default)
    {
        return _channel.BasicCancelAsync(QUEUE_NAME);
    }


    #region // IDisposable and IAsyncDisposable //
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_channel is IAsyncDisposable asyncChannel) { await asyncChannel.DisposeAsync(); }
        else { _channel?.Dispose(); }

        if (_connection is IAsyncDisposable asyncConnection) { await asyncConnection.DisposeAsync(); }
        else { _connection?.Dispose(); }

        _disposed = true;
    }
    #endregion
}

file static class PublishMessageExtensions
{
    private static readonly BasicProperties _defaultProperties = new()
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
    };

    public static ValueTask PublishMessage<T>(this IChannel channel, string exchangeName, T message,
        CancellationToken cancellationToken = default) =>
        channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty,
            mandatory: true,
            basicProperties: _defaultProperties,
            body: message.EncodeMessage(),
            cancellationToken: cancellationToken);

    public static byte[] EncodeMessage<T>(this T message) => message.JsonSerialize().ToUTF8Bytes();
}

file static class BasicDeliverEventArgsExtensions
{
    public static T? GetMessage<T>(this BasicDeliverEventArgs e) => 
        e.Body.ToArray()
        .ToUTF8String()
        .JsonDeserialize<T>();
}