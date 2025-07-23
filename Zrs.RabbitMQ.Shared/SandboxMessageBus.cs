using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zrs.RabbitMQ.Shared.Extensions;

namespace Zrs.RabbitMQ.Shared;

public sealed class SandboxMessageBus : MessageBus<SandboxMessage>
{
    public const string EXCHANGE_NAME = "sandbox";
    public const string QUEUE_NAME = EXCHANGE_NAME + "-queue";




//    public static async Task<SandboxMessageBus> Create(CancellationToken cancellationToken = default)
//    {
//        var messageBus = new SandboxMessageBus();
//        await messageBus.TryConnect(cancellationToken);
//        await messageBus.SetupExchangeAsync(new ExchangeOptions(EXCHANGE_NAME, ZrsExchangeType.Direct), cancellationToken);
//        await messageBus.SetupQueueAsync(new QueueOptions(QUEUE_NAME, EXCHANGE_NAME), cancellationToken);
//        return messageBus;
//    }

//    public static async Task<SandboxMessageBus> CreateConsumer(
//        Func<SandboxMessage, Task> messageHandler,
//        CancellationToken cancellationToken = default)
//    {
//        ArgumentNullException.ThrowIfNull(messageHandler);

//        //var messageBus = await Create(cancellationToken);
//        //messageBus.SetupConsumer(messageHandler);
//        //return messageBus;
//        throw new NotImplementedException("Use Create method instead to initialize the message bus and setup the consumer.");
//    }


//    public SandboxMessageBus() : base() { }


//    public Task PublishMessage(SandboxMessage message, CancellationToken cancellationToken = default) => PublishMessage(message, EXCHANGE_NAME, cancellationToken);




//    private AsyncEventingBasicConsumer? _consumer;
//    private SandboxMessageBus SetupConsumer(Func<SandboxMessage, Task> messageHandler)
//    {
//        _consumer = new AsyncEventingBasicConsumer(Channel);
//        _consumer.ReceivedAsync += async (sender, e) =>
//        {
//            var message = e.GetMessage<SandboxMessage>();
//            if (message is not null) { await messageHandler(message); }
//            await Channel.BasicAckAsync(deliveryTag: e.DeliveryTag, multiple: false);
//        };
//        return this;
//    }


//    public Task StartConsumer(CancellationToken cancellationToken = default) =>
//        _consumer == null
//        ? throw new InvalidOperationException("Consumer not initialized. Use CreateConsumerAsync with a message handler.")
//        : Channel.BasicConsumeAsync(QUEUE_NAME, autoAck: false, _consumer, cancellationToken);

//    public Task StopConsumer(CancellationToken cancellationToken = default) => Channel.BasicCancelAsync(QUEUE_NAME, cancellationToken);
}



//file static class IChannelExtensions
//{
//    public static Task BasicCancelAsync(this IChannel channel, string consumerTag, CancellationToken cancellationToken = default)
//    {
//        ArgumentNullException.ThrowIfNull(channel);
//        return channel.BasicCancelAsync(consumerTag, noWait: false, cancellationToken);
//    }
//}


//file static class BasicDeliverEventArgsExtensions
//{
//    public static T? GetMessage<T>(this BasicDeliverEventArgs e) =>
//        e.Body.ToArray()
//        .ToUTF8String()
//        .JsonDeserialize<T>();
//}