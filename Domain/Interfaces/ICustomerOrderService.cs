using Domain.Entities;
namespace Domain.Interfaces;

public interface ICustomerOrderService
{
    Task<Order?> CheckIdempotencyAsync(string requestId, CancellationToken ct = default);
    Task<Order> CreateOrderAsync(Order request, CancellationToken ct = default);
    Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
    //Task SaveRawPayloadAsync(Order raw, CancellationToken ct = default);
}
