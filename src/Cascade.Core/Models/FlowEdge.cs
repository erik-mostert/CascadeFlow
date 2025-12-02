namespace Cascade.Core.Models;

/// <summary>
/// Represents an edge in a message flow graph, connecting two nodes and describing the message type.
/// </summary>
public record FlowEdge
{
  /// <summary>
  /// Gets the unique identifier of the source node.
  /// </summary>
  public required string SourceId { get; init; }

  /// <summary>
  /// Gets the unique identifier of the target node.
  /// </summary>
  public required string TargetId { get; init; }

  /// <summary>
  /// Gets the label for the edge, typically the short name of the message type.
  /// </summary>
  public required string Label { get; init; }           // Message type short name
}