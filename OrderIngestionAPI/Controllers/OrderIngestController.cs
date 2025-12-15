using Application.Commands.Orders;
using Application.DTOs;
using Application.interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderIngestionAPI.Validators;

namespace OrderIngestionAPI.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrderIngestController : ControllerBase
{
    //private readonly IOrderService _orderService; // From Application Layer
    private readonly ILogger<OrderIngestController> _logger;
    private readonly IMediator _mediator;

    public OrderIngestController(ILogger<OrderIngestController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpPost]
    //[Authorize] // triggers the JWT middleware
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest payload)
    {
        var validator = new OrderIngestRequestValidator();
        var resultValidator = await validator.ValidateAsync(payload);

        if (!resultValidator.IsValid)
        {
            foreach (var error in resultValidator.Errors)
            {
                _logger.LogWarning("Validation failed: {Property} - {ErrorCode} - {ErrorMessage}",
                    error.PropertyName, error.ErrorCode, error.ErrorMessage);
            }

            return BadRequest(resultValidator.Errors);
        }

        //Checking payload
        if (payload == null)
        {
            _logger.LogWarning("Received null payload for order creation.");
            return BadRequest("Payload cannot be null.");
        }

        _logger.LogInformation("Received order creation request. RequestId: {RequestId}, Platform: {Platform}",
            payload.RequestId, payload.Platform);

        try
        {
            // Converting DTO to IngestOrderCommand
            var command = new IngestOrderCommand
            (
                0, // OrderId Will be set by the database
                payload.RequestId,
                0, // CustomerId Will be set by the database
                DateTime.UtcNow,
                0,
                "",
                payload.Platform,
                DateTime.UtcNow,
                DateTime.UtcNow,
                new Customer
                (
                    payload.Customer.Email,
                    payload.Customer.FirstName,
                    payload.Customer.LastName,
                    payload.Customer.Phone
                ),
                payload.Items.Select(i => new OrderItem
                (
                    i.ProductSku,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice
                )).ToList()
            );

            _logger.LogInformation("Creating order. RequestId: {RequestId}, ItemCount: {ItemCount}",
                payload.RequestId, payload.Items.Count);

            //var result = await _orderService.CreateOrderAsync(order);
            //sending order through order service
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Order created successfully. OrderId: {OrderId}, RequestId: {RequestId}, TotalAmount: {TotalAmount}",
                    result.OrderId, result.RequestId, result.TotalAmount);

                return Ok(result);
            }
            else
            {
                _logger.LogError("Order creation failed. RequestId: {RequestId}, Reason: {Reason}",
                    command.RequestId, result.Status);

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
