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
  /// <param name="telemetry">The telemetry data for the message to add.</param>
  /// <returns>The updated or newly created <see cref="MessageFlow"/>.</returns>
  MessageFlow AddMessage(MessageTelemetry telemetry);

  /// <summary>
  /// Gets a specific flow by correlation ID.
  /// </summary>
  /// <param name="correlationId">The correlation identifier of the flow.</param>
  /// <returns>The <see cref="MessageFlow"/> if found; otherwise, <c>null</c>.</returns>
  MessageFlow? GetFlow(string correlationId);

  /// <summary>
  /// Gets all active flows, ordered by most recent first.
  /// </summary>
  /// <returns>An enumerable of active <see cref="MessageFlow"/> instances.</returns>
  IEnumerable<MessageFlow> GetActiveFlows();

  /// <summary>
  /// Asynchronously gets a specific flow from the database by correlation ID.
  /// </summary>
  /// <param name="correlationId">The correlation identifier of the flow.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="MessageFlow"/> if found; otherwise, <c>null</c>.</returns>
  Task<MessageFlow?> GetFlowFromDatabaseAsync(string correlationId);

  /// <summary>
  /// Asynchronously gets flows within a specified time range.
  /// </summary>
  /// <param name="start">The start of the time range.</param>
  /// <param name="end">The end of the time range.</param>
  /// <param name="maxResults">The maximum number of results to return.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of <see cref="MessageFlow"/> instances.</returns>
  Task<IEnumerable<MessageFlow>> GetFlowsInTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int maxResults = 100);

  /// <summary>
  /// Asynchronously searches for flows matching the specified criteria.
  /// </summary>
  /// <param name="endpoint">The endpoint name to filter by, or <c>null</c> for any.</param>
  /// <param name="messageType">The message type to filter by, or <c>null</c> for any.</param>
  /// <param name="hasFailures">Whether to filter by flows with failures, or <c>null</c> for any.</param>
  /// <param name="maxResults">The maximum number of results to return.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of <see cref="MessageFlow"/> instances.</returns>
  Task<IEnumerable<MessageFlow>> SearchFlowsAsync(string? endpoint = null, string? messageType = null, bool? hasFailures = null, int maxResults = 100);

}