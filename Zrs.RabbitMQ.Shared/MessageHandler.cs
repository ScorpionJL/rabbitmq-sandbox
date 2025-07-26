namespace Zrs.RabbitMQ.Shared;

delegate Task RabbitMessageHandler<T>(T message);

public static class RabbitMessageHandlers
{
    /// <summary>
    /// A no-op message handler that does nothing.
    /// </summary>
    public static Task NoOp<T>(T message) => Task.CompletedTask;

    /// <summary>
    /// A message handler that writes the message to the console.
    /// </summary>
    public static Task ConsoleOutput<T>(T message)
    {
        Console.WriteLine(message?.ToString());
        return Task.CompletedTask;
    }
}
