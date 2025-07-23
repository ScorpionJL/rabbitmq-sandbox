// Producer
using Zrs.RabbitMQ.Shared;

var messageBus = await SandboxMessageBus.Create();


// Connection and channel are now established. You can use 'channel' to publish, consume, etc.
for (var i = 0; i < 10; i++)
{
    // Publish a message to the exchange
    var message = new SandboxMessage();
    await messageBus.PublishMessage(message);

    Console.WriteLine($"Sent: {message}");
    await Task.Delay(200);
}
