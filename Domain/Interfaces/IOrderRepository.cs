using Domain.Entities;

namespace Domain.Interfaces;

public interface IOrderRepository
{
    Task<bool> ExistsByExternalIdAsync(string externalOrderId, string source, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task SaveRawPayloadAsync(OrderRaw raw, CancellationToken ct = default);
    Task SaveFailedAsync(OrderRaw raw, string reason, CancellationToken ct = default);
}
