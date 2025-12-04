namespace Cascade.Collector.Data.Entities;

public class StoredEndpoint
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public long MessagesReceived { get; set; }
    public long MessagesSent { get; set; }
    public long Failures { get; set; }
    public double TotalProcessingTimeMs { get; set; }
    public long ProcessingTimeCount { get; set; }
}