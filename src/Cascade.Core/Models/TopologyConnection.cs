namespace Cascade.Core.Models;

/// <summary>
/// A connection between endpoints via a message type.
/// Represents an edge in the topology graph.
/// </summary>
public record TopologyConnection
{
  /// <summary>The endpoint that sent/published the message.</summary>
  public required string SourceEndpoint { get; init; }

  /// <summary>The endpoint that received/handled the message.</summary>
  public required string TargetEndpoint { get; init; }

  /// <summary>The message type flowing through this connection.</summary>
  public required string MessageType { get; init; }

  /// <summary>Short message type name for display.</summary>
  public string MessageTypeShort => MessageType.Split(',')[0].Split('.').LastOrDefault() ?? MessageType;

  /// <summary>Total number of messages sent through this connection.</summary>
  public long MessageCount { get; set; }

  /// <summary>When this connection was first observed.</summary>
  public DateTimeOffset FirstSeen { get; set; }

  /// <summary>When this connection was last observed.</summary>
  public DateTimeOffset LastSeen { get; set; }

  /// <summary>Average latency in milliseconds (if measurable).</summary>
  public double AverageLatencyMs { get; set; }

  /// <summary>Number of failed deliveries on this connection.</summary>
  public long FailureCount { get; set; }

  /// <summary>Failure rate as a percentage (0-1).</summary>
  public double FailureRate => MessageCount > 0 ? (double)FailureCount / MessageCount : 0;

  /// <summary>Unique identifier for this connection.</summary>
  public string Id => $"{SourceEndpoint}|{MessageType}|{TargetEndpoint}";
}