using Cascade.Core.Models;

namespace Cascade.NServiceBus.Dispatchers;

/// <summary>
/// Dispatches telemetry events to the Cascade collector.
/// </summary>
public interface ITelemetryDispatcher : IDisposable
{
  /// <summary>
  /// Queues telemetry for dispatch. Fire-and-forget - never blocks or throws.
  /// </summary>
  Task DispatchAsync(MessageTelemetry telemetry, CancellationToken ct = default);
}