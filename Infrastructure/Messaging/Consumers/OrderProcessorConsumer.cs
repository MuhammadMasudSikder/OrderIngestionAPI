using Application.Commands.Orders;
using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Messaging.Contracts;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Messaging.Consumers;

public class OrderProcessorConsumer : IConsumer<IIngestOrderMessage>
{
    private readonly ICustomerOrderService _repo;
    private readonly ILogger<OrderProcessorConsumer> _logger;
    private readonly ILogisticsGateway _logistics;

    public OrderProcessorConsumer(ICustomerOrderService repo, ILogger<OrderProcessorConsumer> logger, ILogisticsGateway logistics)
    {
        _repo = repo;
        _logger = logger;
        _logistics = logistics;

    }

    public async Task Consume(ConsumeContext<IIngestOrderMessage> context)
    {
        //var msg = context.Message.MsgContext;


        // map payload to domain (simple demo)
        //var json = JsonSerializer.Serialize(context.Message.OrderId);
        _logger.LogInformation("Consuming message {RequestId} OrderId:{OrderId}", context.Message.RequestId, context.Message.OrderId);

        try
        {
            //var order = JsonSerializer.Deserialize<IIngestOrderMessage>(json)!;
            //Simulate a call to a third-party Logistics Gateway
            _logger.LogInformation("Simulate a call to a third-party Logistics Gateway");
            await _logistics.NotifyLogisticsAsync(context.Message.OrderId, context.Message.RequestId);

            await context.RespondAsync(new CreateOrderResponse { Status = "Order forwarded to logistics" });
        }
        catch (DbUpdateException dbex)
        {
            _logger.LogError(dbex, "DB error persisting order {RequestId}", context.Message.RequestId);
            throw; // allow retry
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing order {RequestId}", context.Message.RequestId);
            throw;
        }
    }
}
