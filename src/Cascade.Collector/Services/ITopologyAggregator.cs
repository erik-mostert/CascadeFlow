using Cascade.Core.Models;

namespace Cascade.Collector.Services;

/// <summary>
/// Builds system topology from observed message traffic.
/// </summary>
public interface ITopologyAggregator
{
  /// <summary>
  /// Records a message and updates topology accordingly.
  /// </summary>
  void RecordMessage(MessageTelemetry telemetry);

  /// <summary>
  /// Gets the current system topology.
  /// </summary>
  SystemTopology GetTopology();

  /// <summary>
  /// Resets topology data.
  /// </summary>
  void Reset();
}