using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Zrs.RabbitMQ.Shared;

public sealed class RabbitConsumer<T> : IDisposable, IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly AsyncEventingBasicConsumer _consumer;
    private readonly string _queueName;
    private readonly string _routingKey;


    internal RabbitConsumer(IChannel channel, Func<T, Task> messageHandler, string queueName, string routingKey = "")
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));

        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += async (sender, e) =>
        {
            var message = JsonSerializer.Deserialize<T>(e.Body.Span);
            if (message != null) { await messageHandler(message); }
            await _channel.BasicAckAsync(e.DeliveryTag, multiple: false);
        };

        _queueName = queueName;
        _routingKey = routingKey;
    }


    public Task StartConsumer(CancellationToken cancellationToken = default) =>
        _consumer == null
        ? throw new InvalidOperationException("Consumer not initialized. Use CreateConsumerAsync with a message handler.")
        : _channel.BasicConsumeAsync(_queueName, autoAck: false, _routingKey, _consumer, cancellationToken);
 

    public Task StopConsumer(bool noWait, CancellationToken cancellationToken = default) => 
        _channel.BasicCancelAsync(_queueName, noWait, cancellationToken);
    public Task StopConsumer(CancellationToken cancellationToken = default) => StopConsumer(false, cancellationToken);


    #region // IDisposable and IAsyncDisposable //
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
    public static async Task<RabbitConsumer<T>> CreateConsumer<T>(
        this RabbitConnection connection,
        Func<T, Task> messageHandler,
        string queueName,
        string routingKey = "",
        CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannel(cancellationToken);
        return new RabbitConsumer<T>(channel, messageHandler, queueName, routingKey);
    }

    public static Task<RabbitConsumer<string>> CreateStringConsumer(
        this RabbitConnection connection,
        Func<string, Task> messageHandler,
        string queueName,
        string routingKey = "",
        CancellationToken cancellationToken = default) => 
        connection.CreateConsumer(messageHandler, queueName, routingKey, cancellationToken);
}