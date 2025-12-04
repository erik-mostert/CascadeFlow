namespace Cascade.Collector.Data.Entities;

public class StoredConnection
{
    public int Id { get; set; }
    public required string SourceEndpoint { get; set; }
    public required string TargetEndpoint { get; set; }
    public required string MessageType { get; set; }
    public required string MessageTypeShort { get; set; }
    public long MessageCount { get; set; }
    public long FailureCount { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public double TotalLatencyMs { get; set; }
    public long LatencyCount { get; set; }
}