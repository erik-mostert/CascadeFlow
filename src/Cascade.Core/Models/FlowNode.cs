namespace Cascade.Core.Models;

/// <summary>
/// Represents a node in a message flow graph, corresponding to a message event at an endpoint.
/// </summary>
public record FlowNode
{
  /// <summary>
  /// Gets the unique identifier for the node.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// Gets the label for the node, typically the endpoint name.
  /// </summary>
  public required string Label { get; init; }

  /// <summary>
  /// Gets the full type name of the message associated with this node.
  /// </summary>
  public required string MessageType { get; init; }

  /// <summary>
  /// Gets the timestamp when the message event occurred.
  /// </summary>
  public required DateTimeOffset Timestamp { get; init; }

  /// <summary>
  /// Gets a value indicating whether the message processing succeeded at this node.
  /// </summary>
  public required bool Success { get; init; }

  /// <summary>
  /// Gets the duration of message processing at this node, if available.
  /// </summary>
  public TimeSpan? Duration { get; init; }
}