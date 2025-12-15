namespace Domain.Entities;

public class Order
{
    public int OrderId { get; private set; }
    public string RequestId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; }
    public string? Platform { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Customer Customer { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = new();

    private Order() { } // EF Core

    public Order(
        int orderId,
        string requestId,
        Customer customer,
        IEnumerable<OrderItem> items,
        string? platform)
    {
        OrderId = orderId;
        RequestId = requestId;
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
        Platform = platform;

        OrderDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = "Pending";

        _items.AddRange(items ?? throw new ArgumentNullException(nameof(items)));
        TotalAmount = _items.Sum(i => i.UnitPrice * i.Quantity);
    }
}
