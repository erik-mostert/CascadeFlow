namespace Cascade.Core.Models;

/// <summary>
/// An endpoint (service) discovered in the system topology.
/// </summary>
public record TopologyEndpoint
{
  /// <summary>
  /// Name of the endpoint.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// When this endpoint was first observed.
  /// </summary>
  public DateTimeOffset FirstSeen { get; set; }

  /// <summary>
  /// When this endpoint was last observed.
  /// </summary>
  public DateTimeOffset LastSeen { get; set; }

  /// <summary>
  /// Total number of messages received by this endpoint.
  /// </summary>
  public long MessagesReceived { get; set; }

  /// <summary>
  /// Total number of messages sent by this endpoint.
  /// </summary>
  public long MessagesSent { get; set; }

  /// <summary>
  /// Total number of failed message handlers.
  /// </summary>
  public long Failures { get; set; }

  /// <summary>
  /// Average message processing time in milliseconds.
  /// </summary>
  public double AverageProcessingTimeMs { get; set; }

  /// <summary>
  /// All host instances observed running this endpoint.
  /// </summary>
  public HashSet<string> HostIds { get; init; } = [];

  /// <summary>
  /// Failure rate as a percentage (0-1).
  /// </summary>
  public double FailureRate => MessagesReceived > 0 ? (double)Failures / MessagesReceived : 0;
}