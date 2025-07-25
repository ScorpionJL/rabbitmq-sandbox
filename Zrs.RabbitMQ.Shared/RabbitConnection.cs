using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Zrs.RabbitMQ.Shared;

public sealed class RabbitConnection : IDisposable, IAsyncDisposable
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;


    public RabbitConnection(ConnectionFactory? connectionFactory = null) => 
        _connectionFactory = connectionFactory ?? new ConnectionFactory();


    // factory methods
    public static async Task<RabbitConnection> CreateAsync(
        ConnectionFactory? connectionFactory = null,
        CancellationToken cancellationToken = default)
    {
        var rabbitConnection = new RabbitConnection(connectionFactory ?? new ConnectionFactory());
        if (await rabbitConnection.TryConnect(cancellationToken)) { return rabbitConnection; }
        throw new InvalidOperationException("Could not connect to RabbitMQ server.");
    }


    // connection management
    public bool IsConnected => (_connection?.IsOpen ?? false) && !_disposed;

    public async ValueTask<bool> TryConnect(CancellationToken cancellationToken = default)
    {
        //_logger.LogInformation("RabbitMQ Client is trying to connect");
        if (IsConnected) { return true; }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) { return true; }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);
            _connection.ConnectionShutdownAsync += OnConnectionShutdown;
            _connection.CallbackExceptionAsync += OnCallbackException;
            _connection.ConnectionBlockedAsync += OnConnectionBlocked;

            return true;
        }
        finally { _semaphore.Release(); }
    }


    // topology
    public async Task<IChannel> CreateChannel(CancellationToken cancellationToken = default)
    {
        if (!IsConnected) { throw new InvalidOperationException("Connection to RabbitMQ server is not established."); }
        return await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
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
            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

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
