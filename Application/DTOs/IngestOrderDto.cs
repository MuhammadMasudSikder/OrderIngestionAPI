namespace Application.DTOs;

public record IngestOrderDto(Guid CorrelationId, string Source, string ExternalOrderId, string Payload);
