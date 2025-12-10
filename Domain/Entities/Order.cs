namespace Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
