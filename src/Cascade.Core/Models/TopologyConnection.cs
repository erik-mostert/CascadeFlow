using System;
using System.Linq;
using System.Threading;

namespace Cascade.Core.Models
{
    /// <summary>
    /// A connection between endpoints via a message type.
    /// Represents an edge in the topology graph.
    /// </summary>
    public class TopologyConnection
    {
        private long _messageCount;
        private long _failureCount;

        /// <summary>The endpoint that sent/published the message.</summary>
        public string SourceEndpoint { get; set; } = string.Empty;

        /// <summary>The endpoint that received/handled the message.</summary>
        public string TargetEndpoint { get; set; } = string.Empty;

        /// <summary>The message type flowing through this connection.</summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>Short message type name for display.</summary>
        public string MessageTypeShort => MessageType.Split(',')[0].Split('.').LastOrDefault() ?? MessageType;

        /// <summary>Total number of messages sent through this connection.</summary>
        public long MessageCount
        {
            get => Interlocked.Read(ref _messageCount);
            set => Interlocked.Exchange(ref _messageCount, value);
        }

        /// <summary>When this connection was first observed.</summary>
        public DateTimeOffset FirstSeen { get; set; }

        /// <summary>When this connection was last observed.</summary>
        public DateTimeOffset LastSeen { get; set; }

        /// <summary>Average latency in milliseconds (if measurable).</summary>
        public double AverageLatencyMs { get; set; }

        /// <summary>Number of failed deliveries on this connection.</summary>
        public long FailureCount
        {
            get => Interlocked.Read(ref _failureCount);
            set => Interlocked.Exchange(ref _failureCount, value);
        }

        /// <summary>Failure rate as a percentage (0-1).</summary>
        public double FailureRate => MessageCount > 0 ? (double)FailureCount / MessageCount : 0;

        /// <summary>Unique identifier for this connection.</summary>
        public string Id => $"{SourceEndpoint}|{MessageType}|{TargetEndpoint}";

        /// <summary>
        /// Atomically increments the message count.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementMessageCount() => Interlocked.Increment(ref _messageCount);

        /// <summary>
        /// Atomically increments the failure count.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementFailureCount() => Interlocked.Increment(ref _failureCount);
    }
}
