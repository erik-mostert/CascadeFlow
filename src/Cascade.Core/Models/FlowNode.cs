namespace Cascade.Core.Models;

public record FlowNode
{
  public required string Id { get; init; }
  public required string Label { get; init; }           // Endpoint name
  public required string MessageType { get; init; }
  public required DateTimeOffset Timestamp { get; init; }
  public required bool Success { get; init; }
  public TimeSpan? Duration { get; init; }
}