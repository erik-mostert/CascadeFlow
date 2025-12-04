namespace Cascade.Collector.Data.Entities;

public class StoredMessage
{
    public long Id { get; set; }
    public required string MessageId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ConversationId { get; set; }
    public string? CausationId { get; set; }
    public string? RelatedTo { get; set; }
    public required string MessageType { get; set; }
    public required string MessageTypeShort { get; set; }
    public required string EndpointName { get; set; }
    public required string HostId { get; set; }
    public int Direction { get; set; } // 0 = Incoming, 1 = Outgoing
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan? ProcessingDuration { get; set; }
    public bool? Success { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? OriginatingEndpoint { get; set; }
    public string? SagaId { get; set; }
    public string? SagaType { get; set; }
    public int? RetryCount { get; set; }

    // For quick lookups
    public DateTimeOffset CreatedAt { get; set; }
}