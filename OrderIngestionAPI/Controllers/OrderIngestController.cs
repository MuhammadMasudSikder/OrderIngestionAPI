using Application.Contracts;
using Application.DTOs;
using Application.interfaces;
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
    [Authorize] // triggers the JWT middleware
    public async Task<IActionResult> Ingest([FromBody] OrderIngestRequest payload)
    {
        if (payload == null)
            return BadRequest("Payload cannot be null.");

        try
        {
            var result = await _orderService.IngestOrderAsync(payload);

            if (result.IsSuccess)
                return Ok(result);
            else
                return StatusCode(500, result); // internal server error for failed ingestion
        }
        catch (Exception ex)
        {
            // log the exception here if needed, e.g., _logger.LogError(ex, "Order ingestion failed.");

            var errorResponse = new OrderIngestResponse
            {
                OrderId = payload.ExternalOrderId,
                IsSuccess = false,
                Status = $"Exception: {ex.Message}",
                ProcessedAt = DateTime.UtcNow
            };

            return StatusCode(500, errorResponse);
        }
    }

}
