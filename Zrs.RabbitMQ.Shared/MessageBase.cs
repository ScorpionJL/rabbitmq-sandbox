namespace Zrs.RabbitMQ.Shared;

public record MessageBase
{
    public Guid MessageId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
