namespace Application.Commands;

public record IngestOrderCommand(System.Guid CorrelationId, string Source, string ExternalOrderId, string Payload);
