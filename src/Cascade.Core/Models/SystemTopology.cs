namespace Cascade.Core.Models;

/// <summary>
/// The discovered system topology based on observed message traffic.
/// This is the "living architecture diagram" of your distributed system.
/// </summary>
public class SystemTopology
{
  /// <summary>All discovered endpoints keyed by name.</summary>
  public Dictionary<string, TopologyEndpoint> Endpoints { get; init; } = [];

  /// <summary>All discovered message types keyed by full name.</summary>
  public Dictionary<string, TopologyMessageType> MessageTypes { get; init; } = [];

  /// <summary>All discovered connections between endpoints.</summary>
  public List<TopologyConnection> Connections { get; init; } = [];

  /// <summary>When the first message was observed.</summary>
  public DateTimeOffset FirstObserved { get; set; }

  /// <summary>When the topology was last updated.</summary>
  public DateTimeOffset LastUpdated { get; set; }

  /// <summary>Total number of messages observed across all endpoints.</summary>
  public long TotalMessagesObserved { get; set; }

  /// <summary>Number of unique endpoints discovered.</summary>
  public int EndpointCount => Endpoints.Count;

  /// <summary>Number of unique message types discovered.</summary>
  public int MessageTypeCount => MessageTypes.Count;

  /// <summary>Number of unique connections discovered.</summary>
  public int ConnectionCount => Connections.Count;
}