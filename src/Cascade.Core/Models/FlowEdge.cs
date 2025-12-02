namespace Cascade.Core.Models;

public record FlowEdge
{
  public required string SourceId { get; init; }
  public required string TargetId { get; init; }
  public required string Label { get; init; }           // Message type short name
}