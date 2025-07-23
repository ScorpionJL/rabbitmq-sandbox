using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Zrs.RabbitMQ.Shared;

public sealed class RabbitService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;


    public RabbitService() : this(new ConnectionFactory()) { }

    public RabbitService(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }


    // properties
    public IChannel Channel => _channel ?? throw new InvalidOperationException("Channel is not established.");


    // connection management
    public bool IsConnected => (_connection?.IsOpen ?? false) && (_channel?.IsOpen ?? false) && !_disposed;

    public async Task EnsureConnected(CancellationToken cancellationToken = default)
    {
        if (await TryConnect(cancellationToken)) { return; }
        throw new InvalidOperationException("Could not connect to RabbitMQ server.");
    }

    public async ValueTask<bool> TryConnect(CancellationToken cancellationToken = default)
    {
        //_logger.LogInformation("RabbitMQ Client is trying to connect");
        if (IsConnected) { return true; }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) { return true; }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _connection.ConnectionShutdownAsync += OnConnectionShutdown;
            _connection.CallbackExceptionAsync += OnCallbackException;
            _connection.ConnectionBlockedAsync += OnConnectionBlocked;

            return true;
        }
        finally { _semaphore.Release(); }
    }


    // topology
    public async Task SetupExchangeAsync(ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) { throw new InvalidOperationException("Channel is not established."); }
        await Channel.ExchangeDeclareAsync(
            exchangeOptions.ExchangeName,
            exchangeOptions.ExchangeType,
            durable: true, autoDelete: false, arguments: null,
            cancellationToken: cancellationToken);
    }

    public async Task SetupQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        if (!IsConnected) { throw new InvalidOperationException("Channel is not established."); }
        await Channel.QueueDeclareAsync(
            queueOptions.QueueName,
            durable: true, exclusive: false, autoDelete: false, arguments: null,
            cancellationToken: cancellationToken);
        await Channel.QueueBindAsync(
            queueOptions.QueueName,
            queueOptions.ExchangeName,
            queueOptions.RoutingKey ?? string.Empty,
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

    private void Dispose(bool disposing)
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

    private async ValueTask DisposeAsyncCore()
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
