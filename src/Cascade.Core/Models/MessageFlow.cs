using Cascade.Core.Enums;

namespace Cascade.Core.Models;

public record MessageFlow
{
  public required string CorrelationId { get; init; }
  public required DateTimeOffset StartedAt { get; init; }
  public DateTimeOffset? CompletedAt { get; set; }
  public required List<MessageTelemetry> Messages { get; init; } = new();
  public FlowStatus Status { get; set; } = FlowStatus.InProgress;
  public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;

  // Computed graph structure
  //public List<FlowNode> Nodes => BuildNodes();
  //public List<FlowEdge> Edges => BuildEdges();
}