namespace Application.DTOs;

public class CreateOrderRequest
{
    public string RequestId { get; set; } = string.Empty;
    public CustomerDto Customer { get; set; } = new();
    public string? Platform { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class CustomerDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class OrderItemDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
