namespace Application.DTOs;

public class CreateOrderResponse
{
    public int OrderId { get; set; }
    public bool IsSuccess { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Message { get; set; } = "Order created successfully";
    public DateTime ProcessedAt { get; set; }
}

public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}