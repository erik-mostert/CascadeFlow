namespace Cascade.Collector.Models;

public class FlowImpactMetrics
{
  public required string CorrelationId { get; set; }
  public int TotalMessages { get; set; }
  public int TotalEndpoints { get; set; }
  public int MaxDepth { get; set; }
  public double TotalProcessingTimeMs { get; set; }
  public bool HasFailures { get; set; }
  public List<MessageImpact> MessageTree { get; set; } = new();
  public List<EndpointImpact> EndpointBreakdown { get; set; } = new();
}

public class MessageImpact
{
  public required string MessageId { get; set; }
  public required string MessageType { get; set; }
  public required string PublishedBy { get; set; }
  public int Depth { get; set; }
  public int DownstreamMessageCount { get; set; }
  public int DownstreamEndpointCount { get; set; }
  public List<string> HandledBy { get; set; } = [];
  public List<MessageImpact> Children { get; set; } = [];
}

public class EndpointImpact
{
  public required string EndpointName { get; set; }
  public int MessagesReceived { get; set; }
  public int MessagesPublished { get; set; }
  public int CommandsSent { get; set; }
  public int EventsPublished { get; set; }
  public int RepliesSent { get; set; }
  public double MultiplierRatio { get; set; }
  public double EventMultiplierRatio { get; set; }
  public double ProcessingTimeMs { get; set; }
  public bool HasFailures { get; set; }
}

public class MultiplierEndpoint
{
  public required string EndpointName { get; set; }
  public double MultiplierRatio { get; set; }
  public double EventMultiplierRatio { get; set; }
  public int TotalReceived { get; set; }
  public int TotalPublished { get; set; }
  public int CommandsSent { get; set; }
  public int EventsPublished { get; set; }
  public int SampleSize { get; set; }
  public List<string> CommonOutputMessages { get; set; } = new();
}

public class SystemImpactSummary
{
  public int TotalFlowsAnalyzed { get; set; }
  public double AverageMessagesPerFlow { get; set; }
  public double AverageEndpointsPerFlow { get; set; }
  public double AverageDepth { get; set; }
  public List<MultiplierEndpoint> TopMultipliers { get; set; } = new();
  public List<string> HighImpactMessageTypes { get; set; } = new();
}