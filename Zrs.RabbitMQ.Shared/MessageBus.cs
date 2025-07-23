using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zrs.RabbitMQ.Shared.Extensions;

namespace Zrs.RabbitMQ.Shared;

public class MessageBus<TMessage> : IDisposable, IAsyncDisposable where TMessage : MessageBase
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;


    public MessageBus() : this(new ConnectionFactory()) { }

    public MessageBus(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }


    public IChannel Channel => _channel ?? throw new InvalidOperationException("Channel is not established.");


    // connection management
    public async Task<bool> TryConnect(CancellationToken cancellationToken = default)
    {
        //_logger.LogInformation("RabbitMQ Client is trying to connect");
        if (IsConnected && IsChannelOpen) return true;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _connection.ConnectionShutdownAsync += OnConnectionShutdown;
            _connection.CallbackExceptionAsync += OnCallbackException;
            _connection.ConnectionBlockedAsync += OnConnectionBlocked;

            return true;
        }
        finally { _semaphore.Release(); }
    }

    public bool IsConnected => _connection?.IsOpen ?? false && !_disposed;

    public bool IsChannelOpen => _channel?.IsOpen ?? false && !_disposed;

    public async Task EnsureConnected(CancellationToken cancellationToken = default)
    {
        if (IsConnected && IsChannelOpen) return;
        if (!await TryConnect(cancellationToken))
        {
            throw new InvalidOperationException("Could not connect to RabbitMQ server.");
        }
    }


    // topology
    protected async Task SetupExchangeAsync(ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default) =>
        await Channel.ExchangeDeclareAsync(
            exchange: exchangeOptions.ExchangeName,
            type: exchangeOptions.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

    protected async Task SetupQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        await Channel.QueueDeclareAsync(
            queue: queueOptions.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await Channel.QueueBindAsync(
            queue: queueOptions.QueueName,
            exchange: queueOptions.ExchangeName,
            routingKey: queueOptions.RoutingKey ?? string.Empty,
            cancellationToken: cancellationToken);
    }


    // connection events
    private async Task OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        Console.WriteLine("WARN: A RabbitMQ connection is shutdown. Trying to re-connect...");
        await TryConnect();
    }

    private async Task OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        Console.WriteLine("WARN: A RabbitMQ connection throw exception. Trying to re-connect...");
        await TryConnect();
    }

    private async Task OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        Console.WriteLine("WARN: A RabbitMQ connection is on shutdown. Trying to re-connect...");
        await TryConnect();
    }


    // publish messages
    private static readonly BasicProperties DefaultBasicProperties = new()
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
    };

    protected async Task PublishMessage(TMessage message, string exchangeName, string? routingKey = null, BasicProperties? basicProperties = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName, nameof(exchangeName));

        await EnsureConnected(cancellationToken);

        await _channel!.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey ?? string.Empty,
            mandatory: true,
            basicProperties: basicProperties ?? DefaultBasicProperties,
            body: message.JsonSerialize().ToUTF8Bytes(),
            cancellationToken: cancellationToken);
    }
    protected Task PublishMessage(TMessage message, string exchangeName, CancellationToken cancellationToken = default) => PublishMessage(message, exchangeName, cancellationToken: cancellationToken);


    //private AsyncEventingBasicConsumer? _consumer;
    //protected async Task SetupConsumer()
    //{
    //    await EnsureConnected();

    //    _consumer = new AsyncEventingBasicConsumer(_channel!);
    //    _consumer.ReceivedAsync += async (sender, e) =>
    //    {
    //        var message = e.GetMessage<TMessage>();
    //        if (message is not null) { await OnMessageReceived(message); }
    //        await Channel.BasicAckAsync(deliveryTag: e.DeliveryTag, multiple: false);
    //    };
    //}

    //protected virtual async Task OnMessageReceived(TMessage message)
    //{
    //    // Override this method in derived classes to handle received messages.
    //    // For example, you can log the message or process it further.
    //    await Task.CompletedTask;
    //}

    //public async Task StartConsumer(string queueName, CancellationToken cancellationToken = default)
    //{
    //    ArgumentException.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName));

    //    await SetupConsumer();
    //    await _channel!.BasicConsumeAsync(queueName, autoAck: false, _consumer!, cancellationToken);
    //}

    //public async Task StopConsumer(string queueName, CancellationToken cancellationToken = default)
    //{
    //    ArgumentException.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName));
    //    if (_consumer is null) { return; }
    //    //if (_channel is null) { return; }
    //    await _channel?.BasicCancelAsync(queueName, noWait: false, cancellationToken);
    //}





    #region // IDisposable and IAsyncDisposable //
    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _channel?.Dispose();
            _channel = null;

            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

        if (_channel is not null)
        {
            await _channel.CloseAsync().ConfigureAwait(false);
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
        _disposed = true;
    }
    #endregion
}

file static class BasicDeliverEventArgsExtensions
{
    public static T? GetMessage<T>(this BasicDeliverEventArgs e) =>
        e.Body.ToArray()
        .ToUTF8String()
        .JsonDeserialize<T>();
}