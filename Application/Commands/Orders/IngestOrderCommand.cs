using Application.DTOs;
using Domain.Entities;
using MediatR;

namespace Application.Commands.Orders;

public record IngestOrderCommand(
    //Guid CorrelationId, string Source, string ExternalOrderId, string Payload
     int OrderId,
     string RequestId,
     int CustomerId,
     DateTime OrderDate,
     decimal TotalAmount,
     string Status,
     string? Platform,
     DateTime CreatedAt,
     DateTime UpdatedAt,
     Customer? Customer,
     List<OrderItem> Items
    ) : IRequest<CreateOrderResponse>;
