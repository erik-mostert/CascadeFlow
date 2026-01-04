using System;
using System.Threading;
using System.Threading.Tasks;
using Cascade.Core.Models;

namespace Cascade.NServiceBus.Framework.Dispatchers
{
    /// <summary>
    /// Dispatches telemetry events to the Cascade collector.
    /// Fire-and-forget semantics - never blocks message processing.
    /// </summary>
    public interface ITelemetryDispatcher : IDisposable
    {
        /// <summary>
        /// Queues telemetry for dispatch. Fire-and-forget - never blocks or throws.
        /// </summary>
        Task DispatchAsync(MessageTelemetry telemetry, CancellationToken ct = default);
    }
}
