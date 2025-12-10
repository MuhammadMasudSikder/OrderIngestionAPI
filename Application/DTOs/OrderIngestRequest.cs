namespace Application.DTOs
{
    public class OrderIngestRequest
    {
        public string? ExternalOrderId { get; set; }
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? CustomerEmail { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
