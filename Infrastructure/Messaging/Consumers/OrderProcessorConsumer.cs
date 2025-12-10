using MassTransit;
using Infrastructure.Messaging.Contracts;
using Domain.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Application.Contracts;

namespace Infrastructure.Messaging.Consumers;

public class OrderProcessorConsumer : IConsumer<OrderIngestedMessage>
{
    private readonly IOrderRepository _repo;
    private readonly ILogger<OrderProcessorConsumer> _logger;
    private readonly ILogisticsGateway _logistics;

    public OrderProcessorConsumer(IOrderRepository repo, ILogger<OrderProcessorConsumer> logger, ILogisticsGateway logistics    )
    {
        _repo = repo;
        _logger = logger;
        _logistics = logistics;

    }

    public async Task Consume(ConsumeContext<OrderIngestedMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Consuming message {CorrelationId} ExternalId:{ExternalId}", msg.CorrelationId, msg.ExternalOrderId);

        if (await _repo.ExistsByExternalIdAsync(msg.ExternalOrderId, msg.Source))
        {
            _logger.LogInformation("Duplicate detected. Ignoring. {ExternalId}", msg.ExternalOrderId);
            return;
        }

        // map payload to domain (simple demo)
        var order = new Order
        {
            ExternalOrderId = msg.ExternalOrderId,
            Source = msg.Source,
            CorrelationId = msg.CorrelationId,
            CreatedAt = msg.IngestedAt
        };

        try
        {
            await _repo.AddAsync(order);
            _logger.LogInformation("Order persisted {Id}", order.Id);

            await _logistics.SendToLogisticsAsync(context.Message.ExternalOrderId);
            await context.RespondAsync(new OrderIngestResponse { Status = "Order forwarded to logistics" });
        }
        catch (DbUpdateException dbex)
        {
            _logger.LogError(dbex, "DB error persisting order {ExternalId}", msg.ExternalOrderId);
            throw; // allow retry
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing order {ExternalId}", msg.ExternalOrderId);
            throw;
        }
    }
}
