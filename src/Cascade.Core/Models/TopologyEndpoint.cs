using System;
using System.Collections.Generic;
using System.Threading;

namespace Cascade.Core.Models
{
    /// <summary>
    /// An endpoint (service) discovered in the system topology.
    /// </summary>
    public class TopologyEndpoint
    {
        private long _messagesReceived;
        private long _messagesSent;
        private long _failures;

        /// <summary>
        /// Name of the endpoint.
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
        public long MessagesReceived
        {
            get => Interlocked.Read(ref _messagesReceived);
            set => Interlocked.Exchange(ref _messagesReceived, value);
        }

        /// <summary>
        /// Total number of messages sent by this endpoint.
        /// </summary>
        public long MessagesSent
        {
            get => Interlocked.Read(ref _messagesSent);
            set => Interlocked.Exchange(ref _messagesSent, value);
        }

        /// <summary>
        /// Total number of failed message handlers.
        /// </summary>
        public long Failures
        {
            get => Interlocked.Read(ref _failures);
            set => Interlocked.Exchange(ref _failures, value);
        }

        /// <summary>
        /// Average message processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// All host instances observed running this endpoint.
        /// </summary>
        public HashSet<string> HostIds { get; set; } = new HashSet<string>();

        /// <summary>
        /// Failure rate as a percentage (0-1).
        /// </summary>
        public double FailureRate => MessagesReceived > 0 ? (double)Failures / MessagesReceived : 0;

        /// <summary>
        /// Atomically increments the messages received counter.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementMessagesReceived() => Interlocked.Increment(ref _messagesReceived);

        /// <summary>
        /// Atomically increments the messages sent counter.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementMessagesSent() => Interlocked.Increment(ref _messagesSent);

        /// <summary>
        /// Atomically increments the failures counter.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementFailures() => Interlocked.Increment(ref _failures);
    }
}
