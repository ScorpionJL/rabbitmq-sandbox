// Consumer
using Zrs.RabbitMQ.Shared;

const string EXCHANGE_NAME = "sandbox";
const string QUEUE_NAME = EXCHANGE_NAME + "-queue";

await using var con = await RabbitConnection.CreateAsync();
await con.SetupExchangeAsync(new ExchangeOptions(EXCHANGE_NAME, ZrsExchangeType.Direct));
await con.SetupQueueAsync(new QueueOptions(QUEUE_NAME, EXCHANGE_NAME));

await using var consumer = await con.CreateConsumer<SandboxMessage>(message =>
{
    Console.WriteLine($"Received message: {message}");
    return Task.CompletedTask;
}, QUEUE_NAME);


await consumer.StartConsumer();

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

await consumer.StopConsumer();
