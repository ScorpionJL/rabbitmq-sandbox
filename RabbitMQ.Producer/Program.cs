// Producer
using Zrs.RabbitMQ.Shared;
using Zrs.RabbitMQ.Contracts.Sandbox;

await using var con = await RabbitConnection.CreateAsync();
await using var publisher = await SandboxMessageBus.CreatePublisher(con);

// Connection and channel are now established. You can use 'channel' to publish, consume, etc.
for (var i = 0; i < 10; i++)
{
    // Publish a message to the exchange
    var message = new SandboxMessage();
    await publisher.PublishAsync(message);

    Console.WriteLine($"Sent: {message}");
    await Task.Delay(100);
}
