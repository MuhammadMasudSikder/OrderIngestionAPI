using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Messaging.Contracts;
using Infrastructure.Repositories;
using MassTransit;
using MassTransit.Transports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IPublishEndpoint _publish;
        private readonly ICustomerOrderService _repo;

        public OrderService(ICustomerOrderService repo,
            IPublishEndpoint publish,
            ICustomerOrderService orderRepository,
            ILogisticsGateway logisticsGateway,
            ILogger<OrderService> logger)
        {
            _repo = repo;
            _publish = publish;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> CreateOrderAsync(Order request)
        {
            try
            {
                _logger.LogInformation("Processing order creation request. RequestId: {RequestId}", request.RequestId);

                //Check if order already exists (idempotency)
                var existingOrder = await _repo.CheckIdempotencyAsync(request.RequestId);
                if (existingOrder != null)
                {
                    _logger.LogInformation("Duplicate request detected. Returning existing order. RequestId: {RequestId}, OrderId: {OrderId}",
                        request.RequestId, existingOrder.OrderId);

                    return new CreateOrderResponse
                    {
                        IsSuccess = false,
                        OrderId = existingOrder.OrderId,
                        RequestId = existingOrder.RequestId,
                        Status = existingOrder.Status,
                        TotalAmount = existingOrder.TotalAmount,
                        OrderDate = existingOrder.OrderDate,
                        Message = "Order already processed (idempotent request)"
                    };
                }

                //If not exists, create new order
                var newOrder = await _repo.CreateOrderAsync(request);

                _logger.LogInformation("Order persisted {Id}", newOrder.OrderId);

                //Implement Asynchronous Processing through RabbitMQ, Simulate third-party API call with 2-second delay
                await _publish.Publish<IIngestOrderMessage>(new
                {
                    MsgContext = request
                });

                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, RequestId: {RequestId}",
                    request.OrderId, request.RequestId);

                return new CreateOrderResponse
                {
                    IsSuccess = true,
                    OrderId = newOrder.OrderId,
                    RequestId = newOrder.RequestId,
                    Status = newOrder.Status,
                    TotalAmount = newOrder.TotalAmount,
                    OrderDate = newOrder.OrderDate,
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
