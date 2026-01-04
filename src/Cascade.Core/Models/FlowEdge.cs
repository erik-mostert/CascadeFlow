namespace Cascade.Core.Models
{
    /// <summary>
    /// Represents an edge in a message flow graph, connecting two nodes and describing the message type.
    /// </summary>
    public class FlowEdge
    {
        /// <summary>
        /// Gets or sets the unique identifier of the source node.
        /// </summary>
        public string SourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier of the target node.
        /// </summary>
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the label for the edge, typically the short name of the message type.
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }
}
