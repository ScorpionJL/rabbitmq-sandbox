// Consumer
using Zrs.RabbitMQ.Shared;

using var messageBus = await SandboxMessageBus.CreateConsumerAsync(message =>
{
    Console.WriteLine($"Received message: {message}");
    return Task.CompletedTask;
});

await messageBus.StartConsumer();

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

await messageBus.StopConsumer();
