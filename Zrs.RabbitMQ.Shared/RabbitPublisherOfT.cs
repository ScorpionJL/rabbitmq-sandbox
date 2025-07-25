using System.Runtime.CompilerServices;
using RabbitMQ.Client;

namespace Zrs.RabbitMQ.Shared;

public sealed class RabbitPublisher<T> : IDisposable, IAsyncDisposable
{
    private readonly RabbitPublisher _publisher;
    private readonly string _exchangeName;
    private readonly string _routingKey;


    internal RabbitPublisher(RabbitPublisher publisher, string exchangeName, string routingKey = "") =>
        (_publisher, _exchangeName, _routingKey) = (publisher, exchangeName, routingKey);
    internal RabbitPublisher(IChannel channel, string exchangeName, string routingKey = "") :
        this(new RabbitPublisher(channel), exchangeName, routingKey)
    { }


    public Task PublishAsync(T message, CancellationToken cancellationToken = default)
        => _publisher.PublishAsync(message, _exchangeName, _routingKey, basicProperties: null, cancellationToken);


    #region // IDisposable and IAsyncDisposable implementation //
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _publisher.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _publisher.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
    #endregion
}

public static partial class RabbitConnectionExtensions
{
    public static async Task<RabbitPublisher<T>> CreatePublisher<T>(
        this RabbitConnection connection, 
        string exchangeName,
        string routingKey = "",
        CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannel(cancellationToken);
        return new RabbitPublisher<T>(channel, exchangeName, routingKey);
    }

    public static Task<RabbitPublisher<string>> CreateStringPublisher(
        this RabbitConnection connection,
        string exchangeName,
        string routingKey = "",
        CancellationToken cancellationToken = default) =>
        connection.CreatePublisher<string>(exchangeName, routingKey, cancellationToken);
}
