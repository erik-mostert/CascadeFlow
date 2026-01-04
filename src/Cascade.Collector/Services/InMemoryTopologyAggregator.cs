using System.Collections.Concurrent;
using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Collector.Services;

/// <summary>
/// Builds system topology from observed message traffic.
/// Creates a "living architecture diagram" of your distributed system.
/// </summary>
public class InMemoryTopologyAggregator : ITopologyAggregator
{
  private readonly ConcurrentDictionary<string, TopologyEndpoint> _endpoints = new();
  private readonly ConcurrentDictionary<string, TopologyMessageType> _messageTypes = new();
  private readonly ConcurrentDictionary<string, TopologyConnection> _connections = new();
  private DateTimeOffset _firstObserved = DateTimeOffset.MaxValue;
  private long _totalMessages;

  public void RecordMessage(MessageTelemetry telemetry)
  {
    var now = DateTimeOffset.UtcNow;
    Interlocked.Increment(ref _totalMessages);

    // Track first observation
    if (now < _firstObserved)
    {
      _firstObserved = now;
    }

    // Record the endpoint
    _endpoints.AddOrUpdate(
        telemetry.EndpointName,
        _ => new TopologyEndpoint
        {
          Name = telemetry.EndpointName,
          FirstSeen = now,
          LastSeen = now,
          MessagesReceived = telemetry.Direction == MessageDirection.Incoming ? 1 : 0,
          MessagesSent = telemetry.Direction == MessageDirection.Outgoing ? 1 : 0,
          Failures = telemetry.Success == false ? 1 : 0,
          AverageProcessingTimeMs = telemetry.ProcessingDuration?.TotalMilliseconds ?? 0,
          HostIds = [telemetry.HostId]
        },
        (_, existing) =>
        {
          existing.LastSeen = now;
          existing.HostIds.Add(telemetry.HostId);

          if (telemetry.Direction == MessageDirection.Incoming)
          {
            var newCount = existing.IncrementMessagesReceived();
            if (telemetry.Success == false)
              existing.IncrementFailures();

            // Update running average for processing time
            if (telemetry.ProcessingDuration.HasValue)
            {
              var totalTime = existing.AverageProcessingTimeMs * (newCount - 1);
              existing.AverageProcessingTimeMs = (totalTime + telemetry.ProcessingDuration.Value.TotalMilliseconds) / newCount;
            }
          }
          else
          {
            existing.IncrementMessagesSent();
          }

          return existing;
        });

    // Record the message type
    _messageTypes.AddOrUpdate(
        telemetry.MessageType,
        _ => new TopologyMessageType
        {
          FullName = telemetry.MessageType,
          FirstSeen = now,
          LastSeen = now,
          TimesObserved = 1
        },
        (_, existing) =>
        {
          existing.LastSeen = now;
          existing.IncrementTimesObserved();
          return existing;
        });

    // Record connections (edges in the topology graph)
    // For incoming messages, create edge: OriginatingEndpoint -> CurrentEndpoint
    if (telemetry.Direction == MessageDirection.Incoming &&
        !string.IsNullOrEmpty(telemetry.OriginatingEndpoint) &&
        telemetry.OriginatingEndpoint != telemetry.EndpointName)
    {
      var connectionKey = $"{telemetry.OriginatingEndpoint}|{telemetry.MessageType}|{telemetry.EndpointName}";

      _connections.AddOrUpdate(
          connectionKey,
          _ => new TopologyConnection
          {
            SourceEndpoint = telemetry.OriginatingEndpoint,
            TargetEndpoint = telemetry.EndpointName,
            MessageType = telemetry.MessageType,
            FirstSeen = now,
            LastSeen = now,
            MessageCount = 1,
            FailureCount = telemetry.Success == false ? 1 : 0
          },
          (_, existing) =>
          {
            existing.LastSeen = now;
            existing.IncrementMessageCount();
            if (telemetry.Success == false)
              existing.IncrementFailureCount();
            return existing;
          });
    }
  }

  public SystemTopology GetTopology()
  {
    return new SystemTopology
    {
      Endpoints = new Dictionary<string, TopologyEndpoint>(_endpoints),
      MessageTypes = new Dictionary<string, TopologyMessageType>(_messageTypes),
      Connections = _connections.Values.ToList(),
      FirstObserved = _firstObserved == DateTimeOffset.MaxValue ? DateTimeOffset.UtcNow : _firstObserved,
      LastUpdated = DateTimeOffset.UtcNow,
      TotalMessagesObserved = _totalMessages
    };
  }

  public void Reset()
  {
    _endpoints.Clear();
    _messageTypes.Clear();
    _connections.Clear();
    _firstObserved = DateTimeOffset.MaxValue;
    Interlocked.Exchange(ref _totalMessages, 0);
  }
}