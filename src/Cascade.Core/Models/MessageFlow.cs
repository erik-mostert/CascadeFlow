using Cascade.Core.Enums;

namespace Cascade.Core.Models;

/// <summary>
/// A correlated flow of messages sharing the same CorrelationId.
/// Represents a complete transaction across the distributed system.
/// </summary>
public record MessageFlow
{
  /// <summary>The shared correlation ID linking all messages in this flow.</summary>
  public required string CorrelationId { get; init; }

  /// <summary>When the first message in this flow was observed.</summary>
  public required DateTimeOffset StartedAt { get; init; }

  /// <summary>When the flow was considered complete (if applicable).</summary>
  public DateTimeOffset? CompletedAt { get; set; }

  /// <summary>All messages belonging to this flow.</summary>
  public required List<MessageTelemetry> Messages { get; init; } = [];

  /// <summary>Current status of the flow.</summary>
  public FlowStatus Status { get; set; } = FlowStatus.InProgress;

  /// <summary>Total duration of the flow.</summary>
  public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;

  /// <summary>Number of messages in this flow.</summary>
  public int MessageCount => Messages.Count;

  /// <summary>Whether any message in the flow failed.</summary>
  public bool HasFailures => Messages.Any(m => m.Success == false);
}