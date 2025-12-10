namespace Infrastructure.Messaging.Contracts;

public class OrderIngestedMessage
{
    public System.Guid CorrelationId { get; set; }
    public string Source { get; set; } = null!;
    public string ExternalOrderId { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public System.DateTime IngestedAt { get; set; } = System.DateTime.UtcNow;
}
