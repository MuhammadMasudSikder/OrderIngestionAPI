using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogisticsGateway _logisticsGateway;
        private readonly ILogger<OrderService> _logger;
        private readonly IPublishEndpoint _publish;

        public OrderService(
            IPublishEndpoint publish,
            IOrderRepository orderRepository,
            ILogisticsGateway logisticsGateway,
            ILogger<OrderService> logger)
        {
            _publish = publish;
            _logisticsGateway = logisticsGateway;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> CreateOrderAsync(Order request)
        {
            try
            {
                _logger.LogInformation("Processing order creation request. RequestId: {RequestId}", request.RequestId);

                


                await _publish.Publish(request);



                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, RequestId: {RequestId}",
                    request.OrderId, request.RequestId);

                return new CreateOrderResponse
                {
                    OrderId = request.OrderId,
                    RequestId = request.RequestId,
                    Status = request.Status,
                    TotalAmount = request.TotalAmount,
                    OrderDate = request.OrderDate,
                    Message = "Order created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order creation. RequestId: {RequestId}", request.RequestId);
                // return failure response
                return new CreateOrderResponse
                {
                    RequestId = request.RequestId,
                    IsSuccess = false,
                    Status = $"Failed: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }
    }
}
