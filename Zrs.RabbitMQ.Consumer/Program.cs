﻿// Consumer
using Zrs.RabbitMQ.Shared;
using Zrs.RabbitMQ.Contracts.Sandbox;

await using var con = await RabbitConnection.CreateAsync();
await using var consumer = await SandboxMessageBus.CreateConsumer(con, RabbitMessageHandlers.ConsoleOutput);


await consumer.StartConsumer();

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

await consumer.StopConsumer();
