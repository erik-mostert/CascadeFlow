using System;

namespace Cascade.Core.Models
{
    /// <summary>
    /// Represents a node in a message flow graph, corresponding to a message event at an endpoint.
    /// </summary>
    public class FlowNode
    {
        /// <summary>
        /// Gets or sets the unique identifier for the node.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the label for the node, typically the endpoint name.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full type name of the message associated with this node.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message processing succeeded at this node.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the duration of message processing at this node, if available.
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}
