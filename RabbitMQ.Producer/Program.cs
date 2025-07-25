// Producer
using Zrs.RabbitMQ.Shared;

const string EXCHANGE_NAME = "sandbox";
const string QUEUE_NAME = EXCHANGE_NAME + "-queue";

await using var con = await RabbitConnection.CreateAsync();
await con.SetupExchangeAsync(new ExchangeOptions(EXCHANGE_NAME, ZrsExchangeType.Direct));
await con.SetupQueueAsync(new QueueOptions(QUEUE_NAME, EXCHANGE_NAME));

await using var publisher = await con.CreatePublisher<SandboxMessage>(EXCHANGE_NAME);

// Connection and channel are now established. You can use 'channel' to publish, consume, etc.
for (var i = 0; i < 10; i++)
{
    // Publish a message to the exchange
    var message = new SandboxMessage();
    await publisher.PublishAsync(message);

    Console.WriteLine($"Sent: {message}");
    await Task.Delay(100);
}
