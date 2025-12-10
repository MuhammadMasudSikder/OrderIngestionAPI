using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderIngestionAPI.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrderIngestController : ControllerBase
{
    private readonly IOrderService _orderService; // From Application Layer
    private readonly ILogger<OrderIngestController> _logger;

    public OrderIngestController(IOrderService orderService, ILogger<OrderIngestController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    //[Authorize] // triggers the JWT middleware
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest payload)
    {
        if (payload == null)
        {
            _logger.LogWarning("Received null payload for order creation.");
            return BadRequest("Payload cannot be null.");
        }

        _logger.LogInformation("Received order creation request. RequestId: {RequestId}, Platform: {Platform}",
            payload.RequestId, payload.Platform);

        try
        {
            // Converting DTO to Domain Entity
            var order = new Order
            {
                RequestId = payload.RequestId,
                Platform = payload.Platform,
                Customer = payload.Customer != null ? new Customer
                {
                    Email = payload.Customer.Email,
                    FirstName = payload.Customer.FirstName,
                    LastName = payload.Customer.LastName,
                    Phone = payload.Customer.Phone,
                } : null,
                Items = payload.Items.Select(i => new OrderItem
                {
                    ProductSku = i.ProductSku,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                }).ToList()
            };

            _logger.LogInformation("Creating order. RequestId: {RequestId}, ItemCount: {ItemCount}",
                payload.RequestId, order.Items.Count);

            var result = await _orderService.CreateOrderAsync(order);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, RequestId: {RequestId}, TotalAmount: {TotalAmount}",
                    result.OrderId, result.RequestId, result.TotalAmount);

                return Ok(result);
            }
            else
            {
                _logger.LogError("Order creation failed. RequestId: {RequestId}, Reason: {Reason}",
                    payload.RequestId, result.Status);

                return StatusCode(500, result); // Internal server error
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while processing order creation. RequestId: {RequestId}", payload.RequestId);

            var errorResponse = new CreateOrderResponse
            {
                RequestId = payload.RequestId,
                IsSuccess = false,
                Status = $"Exception: {ex.Message}",
                OrderDate = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }
}
