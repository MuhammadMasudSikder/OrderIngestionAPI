namespace Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string ExternalOrderId { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? CustomerEmail { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CorrelationId { get; set; }
}
