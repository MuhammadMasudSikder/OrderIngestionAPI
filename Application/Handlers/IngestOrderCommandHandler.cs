using Application.Commands.Orders;
using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Domain.Interfaces;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Handlers
{
    public class IngestOrderCommandHandler
    : IRequestHandler<IngestOrderCommand, CreateOrderResponse>
    {
        private readonly ILogger<IngestOrderCommandHandler> _logger;
        private readonly IPublishEndpoint _publish;
        private readonly ICustomerOrderService _orderService;

        public IngestOrderCommandHandler(ICustomerOrderService orderService,
            IPublishEndpoint publish,
            ILogger<IngestOrderCommandHandler> logger)
        {
            _orderService = orderService;
            _publish = publish;
            _logger = logger;
        }

        public async Task<CreateOrderResponse> Handle(
            IngestOrderCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing order creation request. RequestId: {RequestId}", request.RequestId);

                //Check if order already exists (idempotency)
                var existingOrder = await _orderService.CheckIdempotencyAsync(request.RequestId);
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

                var customer = new Customer(
                    request.Customer.Email,
                    request.Customer.FirstName,
                    request.Customer.LastName,
                    request.Customer.Phone
                );

                var items = request.Items.Select(i =>
                    new OrderItem(
                        i.ProductSku,
                        i.ProductName,
                        i.Quantity,
                        i.UnitPrice
                    )
                ).ToList();

                var order = new Order(
                    orderId: request.OrderId,
                    requestId: request.RequestId,
                    customer: customer,
                    items: items,
                    platform: request.Platform
                );

                //If not exists, create new order
                var newOrder = await _orderService.CreateOrderAsync(order);

                if (newOrder != null)
                {
                    _logger.LogInformation("Order persisted {Id}", newOrder.OrderId);

                    //Implement Asynchronous Processing through RabbitMQ, Simulate third-party API call with 2-second delay
                    await _publish.Publish<IIngestOrderMessage>(new
                    {
                        OrderId = newOrder.OrderId,
                        RequestId = newOrder.RequestId,
                        Status = newOrder.Status,
                        TotalAmount = newOrder.TotalAmount
                    });

                    _logger.LogInformation("Order created successfully. OrderId: {OrderId}, RequestId: {RequestId}",
                        newOrder.OrderId, newOrder.RequestId);

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
                return new CreateOrderResponse
                {
                    IsSuccess = false,
                    RequestId = request.RequestId,
                    Status = "Failed to create order",
                    OrderDate = DateTime.UtcNow
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
