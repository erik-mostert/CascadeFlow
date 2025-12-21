using System.Collections.Concurrent;
using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Collector.Services;

/// <summary>
/// In-memory implementation of flow aggregation.
/// Groups messages by CorrelationId and manages flow lifecycle.
/// </summary>
public class InMemoryFlowAggregator : IFlowAggregator
{
  private readonly ConcurrentDictionary<string, MessageFlow> _flows = new();
  private readonly TimeSpan _flowTimeout = TimeSpan.FromMinutes(5);
  private readonly int _maxFlows = 1000;

  /// <inheritdoc/>
  public MessageFlow AddMessage(MessageTelemetry telemetry)
  {
    var correlationId = telemetry.CorrelationId ?? telemetry.MessageId;
    var now = DateTimeOffset.UtcNow;  // Use server time for tracking

    var flow = _flows.AddOrUpdate(
        correlationId,
        _ => new MessageFlow
        {
          CorrelationId = correlationId,
          StartedAt = now,  // Use current time, not message timestamp
          Messages = [telemetry],
          Status = telemetry.Success == false ? FlowStatus.Failed : FlowStatus.InProgress
        },
        (_, existing) =>
        {
          existing.Messages.Add(telemetry);
          UpdateFlowStatus(existing);
          return existing;
        });

    CleanupOldFlows();

    return flow;
  }

  /// <inheritdoc/>
  public MessageFlow? GetFlow(string correlationId)
  {
    return _flows.TryGetValue(correlationId, out var flow) ? flow : null;
  }

  /// <inheritdoc/>
  public IEnumerable<MessageFlow> GetActiveFlows()
  {
    return _flows.Values
        .OrderByDescending(f => f.StartedAt)
        .Take(100);
  }

  /// <inheritdoc/>
  public Task<MessageFlow?> GetFlowFromDatabaseAsync(string correlationId)
  {
    // In-memory doesn't have a database, just return from memory
    return Task.FromResult(GetFlow(correlationId));
  }

  /// <inheritdoc/>
  public Task<IEnumerable<MessageFlow>> GetFlowsInTimeRangeAsync(DateTimeOffset start, DateTimeOffset end, int maxResults = 100)
  {
    var flows = _flows.Values
        .Where(f => f.StartedAt >= start && f.StartedAt <= end)
        .OrderByDescending(f => f.StartedAt)
        .Take(maxResults);

    return Task.FromResult(flows);
  }

  /// <inheritdoc/>
  public Task<IEnumerable<MessageFlow>> SearchFlowsAsync(string? endpoint = null, string? messageType = null, bool? hasFailures = null, int maxResults = 100)
  {
    var query = _flows.Values.AsEnumerable();

    if (!string.IsNullOrEmpty(endpoint))
    {
      query = query.Where(f => f.Messages.Any(m => m.EndpointName.Contains(endpoint, StringComparison.OrdinalIgnoreCase)));
    }

    if (!string.IsNullOrEmpty(messageType))
    {
      query = query.Where(f => f.Messages.Any(m => 
        m.MessageTypeShort != null &&
        m.MessageTypeShort.Contains(messageType, StringComparison.OrdinalIgnoreCase)));
    }

    if (hasFailures.HasValue)
    {
      query = query.Where(f => f.HasFailures == hasFailures.Value);
    }

    var results = query
        .OrderByDescending(f => f.StartedAt)
        .Take(maxResults);

    return Task.FromResult(results);
  }

  private void UpdateFlowStatus(MessageFlow flow)
  {
    // If any message failed, mark the flow as failed
    if (flow.Messages.Any(m => m.Success == false))
    {
      flow.Status = FlowStatus.Failed;
    }

    // Check for timeout
    var age = DateTimeOffset.UtcNow - flow.StartedAt;
    if (age > _flowTimeout && flow.Status == FlowStatus.InProgress)
    {
      flow.Status = FlowStatus.TimedOut;
      flow.CompletedAt = DateTimeOffset.UtcNow;
    }
  }

  private void CleanupOldFlows()
  {
    // Remove flows older than timeout
    var cutoff = DateTimeOffset.UtcNow - _flowTimeout;
    var oldFlows = _flows
        .Where(kvp => kvp.Value.StartedAt < cutoff)
        .Select(kvp => kvp.Key)
        .ToList();

    foreach (var key in oldFlows)
    {
      _flows.TryRemove(key, out _);
    }

    // If still over limit, remove oldest
    if (_flows.Count > _maxFlows)
    {
      var toRemove = _flows
          .OrderBy(kvp => kvp.Value.StartedAt)
          .Take(_flows.Count - _maxFlows)
          .Select(kvp => kvp.Key)
          .ToList();

      foreach (var key in toRemove)
      {
        _flows.TryRemove(key, out _);
      }
    }
  }
}