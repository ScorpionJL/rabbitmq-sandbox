using RabbitMQ.Client;
using Zrs.RabbitMQ.Shared.Extensions;

namespace Zrs.RabbitMQ.Shared;

public class RabbitPublisher<TMessage> where TMessage : MessageBase
{
    private readonly RabbitService _rabbitClient;

    public RabbitPublisher() : this(new ConnectionFactory()) { }
    public RabbitPublisher(ConnectionFactory connectionFactory)
    {
        _rabbitClient = new RabbitService(connectionFactory);
    }


    public async ValueTask<bool> TryConnect(CancellationToken cancellationToken = default) => await _rabbitClient.TryConnect(cancellationToken);


    // publish messages
    private static readonly BasicProperties DefaultBasicProperties = new()
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
    };

    protected async Task PublishMessage(
        TMessage message,
        string exchangeName,
        string? routingKey = null,
        BasicProperties? basicProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName, nameof(exchangeName));

        await _rabbitClient.EnsureConnected(cancellationToken);

        await _rabbitClient.Channel.BasicPublishAsync(
            exchangeName,
            routingKey ?? string.Empty,
            mandatory: true,
            basicProperties ?? DefaultBasicProperties,
            body: message.JsonSerialize().ToUTF8Bytes(),
            cancellationToken);
    }
    protected Task PublishMessage(TMessage message, string exchangeName, CancellationToken cancellationToken = default) =>
        PublishMessage(message, exchangeName, cancellationToken: cancellationToken);
}
