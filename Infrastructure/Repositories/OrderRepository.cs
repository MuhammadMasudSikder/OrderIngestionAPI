using Dapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnection _db;

    public OrderRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalOrderId, string source, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@ExternalOrderId", externalOrderId);
        parameters.Add("@Source", source);

        // SQL stored procedure returns BIT
        var result = await _db.ExecuteScalarAsync<int>(
            "sp_OrderExistsByExternalId",        
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return result == 1;
    }

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CorrelationId", order.CorrelationId);
        parameters.Add("@ExternalOrderId", order.ExternalOrderId);
        parameters.Add("@Source", order.Source);
        parameters.Add("@Amount", order.Amount);
        parameters.Add("@CustomerEmail", order.CustomerEmail);

        // Assuming stored procedure returns new OrderId
        parameters.Add("@NewOrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await _db.ExecuteAsync(
            "sp_AddOrder",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        order.Id = parameters.Get<int>("@NewOrderId");
        return order;
    }

    public async Task SaveRawPayloadAsync(OrderRaw raw, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CorrelationId", raw.CorrelationId);
        parameters.Add("@ExternalOrderId", raw.ExternalOrderId);
        parameters.Add("@Source", raw.Source);
        parameters.Add("@Payload", raw.Payload);

        await _db.ExecuteAsync(
            "sp_SaveOrderRaw",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task SaveFailedAsync(OrderRaw raw, string reason, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@ExternalOrderId", raw.ExternalOrderId);
        parameters.Add("@Source", raw.Source);

        var failedPayload = raw.Payload + "\n\nFailedReason:\t" + reason;
        parameters.Add("@Payload", failedPayload);

        await _db.ExecuteAsync(
            "sp_SaveOrderRawFailed",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
