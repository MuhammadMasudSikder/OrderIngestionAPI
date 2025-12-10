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

    public OrderIngestController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    //[Authorize] // triggers the JWT middleware
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest payload)
    {
        if (payload == null)
            return BadRequest("Payload cannot be null.");

        try
        {
            //Converting DTO to Domain Entity internal Order instance
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

            var result = await _orderService.CreateOrderAsync(order);

            if (result.IsSuccess)
                return Ok(result);
            else
                return StatusCode(500, result); // internal server error for failed ingestion
        }
        catch (Exception ex)
        {
            // log the exception here if needed, e.g., _logger.LogError(ex, "Order ingestion failed.");

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
