namespace Domain.Entities;

public class OrderRaw
{
    public int Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string? ExternalOrderId { get; set; } = null!;
    public string? Source { get; set; } = null!;
    public string? Payload { get; set; } = null!;
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
