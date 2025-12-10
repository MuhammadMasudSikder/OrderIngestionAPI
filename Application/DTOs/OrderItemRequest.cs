namespace Application.DTOs
{
    public class OrderItemRequest
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
