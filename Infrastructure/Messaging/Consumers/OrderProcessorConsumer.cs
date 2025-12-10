using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Messaging.Contracts;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Messaging.Consumers;

public class OrderProcessorConsumer : IConsumer<OrderIngestedMessage>
{
    private readonly IOrderRepository _repo;
    private readonly ILogger<OrderProcessorConsumer> _logger;
    private readonly ILogisticsGateway _logistics;

    public OrderProcessorConsumer(IOrderRepository repo, ILogger<OrderProcessorConsumer> logger, ILogisticsGateway logistics)
    {
        _repo = repo;
        _logger = logger;
        _logistics = logistics;

    }

    public async Task Consume(ConsumeContext<OrderIngestedMessage> context)
    {
        var msg = context.Message.MsgContext;
        _logger.LogInformation("Consuming message {RequestId} OrderId:{OrderId}", msg.RequestId, msg.OrderId);

        // map payload to domain (simple demo)
        var json = JsonSerializer.Serialize(msg);
        var order = JsonSerializer.Deserialize<Order>(json)!;

        try
        {
            // Create the order
            await _repo.CreateOrderAsync(order);
            _logger.LogInformation("Order persisted {Id}", order.OrderId);

            //Simulate a call to a third-party Logistics Gateway
            _logger.LogInformation("Simulate a call to a third-party Logistics Gateway");
            await _logistics.NotifyLogisticsAsync(order.OrderId, order.RequestId);

            await context.RespondAsync(new CreateOrderResponse { Status = "Order forwarded to logistics" });
        }
        catch (DbUpdateException dbex)
        {
            _logger.LogError(dbex, "DB error persisting order {RequestId}", msg.RequestId);
            throw; // allow retry
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing order {RequestId}", msg.RequestId);
            throw;
        }
    }
}
