namespace Domain.Entities;

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    private OrderItem() { }

    public OrderItem(
            string productSku,
            string productName,
            int quantity,
            decimal unitPrice)
    {
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
