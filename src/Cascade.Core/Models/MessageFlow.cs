using Cascade.Core.Enums;

namespace Cascade.Core.Models;

/// <summary>
/// Represents the flow of related messages within a correlated process or conversation.
/// </summary>
public record MessageFlow
{
  /// <summary>
  /// Gets the correlation identifier that links related messages in the flow.
  /// </summary>
  public required string CorrelationId { get; init; }

  /// <summary>
  /// Gets the timestamp when the message flow started.
  /// </summary>
  public required DateTimeOffset StartedAt { get; init; }

  /// <summary>
  /// Gets or sets the timestamp when the message flow completed, if available.
  /// </summary>
  public DateTimeOffset? CompletedAt { get; set; }

  /// <summary>
  /// Gets the collection of telemetry data for all messages in the flow.
  /// </summary>
  public required List<MessageTelemetry> Messages { get; init; } = new();

  /// <summary>
  /// Gets or sets the current status of the message flow.
  /// </summary>
  public FlowStatus Status { get; set; } = FlowStatus.InProgress;

  /// <summary>
  /// Gets the duration of the message flow, calculated from <see cref="StartedAt"/> to <see cref="CompletedAt"/> (or now if not completed).
  /// </summary>
  public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;

  // Computed graph structure
  //public List<FlowNode> Nodes => BuildNodes();
  //public List<FlowEdge> Edges => BuildEdges();
}