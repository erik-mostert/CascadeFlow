using Cascade.Core.Models;

namespace Cascade.Collector.Services;

/// <summary>
/// Aggregates message telemetry into correlated flows.
/// </summary>
public interface IFlowAggregator
{
  /// <summary>
  /// Adds a message to its corresponding flow (or creates a new flow).
  /// </summary>
  MessageFlow AddMessage(MessageTelemetry telemetry);

  /// <summary>
  /// Gets a specific flow by correlation ID.
  /// </summary>
  MessageFlow? GetFlow(string correlationId);

  /// <summary>
  /// Gets all active flows, ordered by most recent first.
  /// </summary>
  IEnumerable<MessageFlow> GetActiveFlows();
}