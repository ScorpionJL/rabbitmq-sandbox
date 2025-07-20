using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Zrs.RabbitMQ.Producer;
internal sealed class RabbitMQConnection : IDisposable
{
    private static readonly SemaphoreSlim semaphore = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task<bool> TryConnect()
    {
        //_logger.LogInformation("RabbitMQ Client is trying to connect");

        await semaphore.WaitAsync();
        try
        {
            var factory = new ConnectionFactory();
            _connection = await factory.CreateConnectionAsync();

            if (this.IsConnected)
            {
                _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                _connection.CallbackExceptionAsync += OnCallbackException;
                _connection.ConnectionBlockedAsync += OnConnectionBlocked;

                //_logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);
                _channel = await _connection.CreateChannelAsync();
                return true;
            }
            else
            {
                //_logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }
        finally { semaphore.Release(); }

    }

    public bool IsConnected => _connection?.IsOpen ?? false && !_disposed;

    public IConnection Connection => _connection ?? throw new InvalidOperationException("Connection is not established.");
    public IChannel Channel => _channel ?? throw new InvalidOperationException("Channel is not established.");


    private async Task OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        //_logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");
        await TryConnect();
    }

    private async Task OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        //_logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");
        await TryConnect();
    }

    private async Task OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        //_logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        await TryConnect();
    }


    #region // IDisposable //
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~RabbitMQConnection() => Dispose(false);


    private void Dispose(bool disposeManaged)
    {
        if (_disposed) return;

        if (disposeManaged)
        {
            // Dispose managed resources here
            _channel?.Dispose();
            _connection?.Dispose();
        }

        // Free unmanaged resources here

        // and we are done
        _disposed = true;
    }
    #endregion
}
