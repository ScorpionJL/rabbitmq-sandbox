using RabbitMQ.Client;
using Zrs.RabbitMQ.Shared.Extensions;

namespace Zrs.RabbitMQ.Shared;

public sealed class RabbitPublisher : IDisposable, IAsyncDisposable
{
    private readonly IChannel _channel;

    internal RabbitPublisher(IChannel channel) => 
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));

    private static readonly BasicProperties DefaultBasicProperties = new()
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
    };

    public async Task PublishAsync<T>(
        T message,
        string exchangeName,
        string routingKey = "",
        BasicProperties? basicProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName, nameof(exchangeName));

        await _channel.BasicPublishAsync(
            exchangeName,
            routingKey,
            mandatory: true,
            basicProperties ?? DefaultBasicProperties,
            body: message.JsonSerialize().ToUTF8Bytes(),
            cancellationToken);
    }


    #region // IDisposable and IAsyncDisposable implementation //
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _channel.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _channel.CloseAsync().ConfigureAwait(false);
        await _channel.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
    #endregion
}

public static partial class RabbitConnectionExtensions
{
    public static async Task<RabbitPublisher> CreatePublisher(
        this RabbitConnection connection,
        CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannel(cancellationToken);
        return new RabbitPublisher(channel);
    }
}
