using Application.Contracts;
using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Messaging.Contracts;
using MassTransit;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IPublishEndpoint _publish;

        public OrderService(IPublishEndpoint publish, IOrderRepository repo)
        {
            _publish = publish;
            _repo = repo;
        }

        public async Task<OrderIngestResponse> IngestOrderAsync(OrderIngestRequest dto)
        {
            try
            {
                // generate correlation id
                var corr = Guid.NewGuid();

                // persist raw payload for audit
                var raw = new OrderRaw
                {
                    CorrelationId = corr,
                    ExternalOrderId = dto.ExternalOrderId,
                    Payload = dto.ToString(),
                    IngestedAt = DateTime.UtcNow
                };

                await _repo.SaveRawPayloadAsync(raw);

                // publish message to bus
                var msg = new OrderIngestedMessage
                {
                    CorrelationId = corr,
                    ExternalOrderId = dto.ExternalOrderId,
                    Payload = dto.ToString(),
                    IngestedAt = DateTime.UtcNow
                };

                await _publish.Publish(msg);

                // return success response
                return new OrderIngestResponse
                {
                    OrderId = dto.ExternalOrderId,
                    IsSuccess = true,
                    Status = "Accepted",
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                // log the exception if needed
                // return failure response
                return new OrderIngestResponse
                {
                    OrderId = dto.ExternalOrderId,
                    IsSuccess = false,
                    Status = $"Failed: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

    }
}
