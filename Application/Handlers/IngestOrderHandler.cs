using Domain.Interfaces;
using Domain.Entities;
using Application.Commands;

namespace Application.Handlers;

public class IngestOrderHandler
{
    private readonly IOrderRepository _repo;

    public IngestOrderHandler(IOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(IngestOrderCommand cmd, CancellationToken ct = default)
    {
        // Basic business-level validation example
        if (string.IsNullOrWhiteSpace(cmd.ExternalOrderId))
            throw new ArgumentException("ExternalOrderId required");

        var raw = new OrderRaw
        {
            CorrelationId = cmd.CorrelationId,
            ExternalOrderId = cmd.ExternalOrderId,
            Source = cmd.Source,
            Payload = cmd.Payload,
            IngestedAt = DateTime.UtcNow
        };

        await _repo.SaveRawPayloadAsync(raw, ct);
    }
}
