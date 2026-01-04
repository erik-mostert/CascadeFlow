using System;
using System.Collections.Generic;
using System.Linq;
using Cascade.Core.Enums;

namespace Cascade.Core.Models
{
    /// <summary>
    /// Represents telemetry data for an NServiceBus message event.
    /// </summary>
    public class MessageTelemetry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the telemetry event.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier assigned to the message by NServiceBus.
        /// </summary>
        /// <remarks>The message ID is used to track and correlate messages within the messaging infrastructure. This
        /// value is set by NServiceBus when the message is sent and remains unchanged throughout the message's
        /// lifecycle.</remarks>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation identifier for linking related messages.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the conversation associated with the message.
        /// </summary>
        /// <remarks>This identifier is used to correlate related messages within a single logical conversation in
        /// NServiceBus. The value may be null if the message is not part of a conversation.</remarks>
        public string? ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the causation identifier, which references the direct parent message.
        /// </summary>
        public string? CausationId { get; set; }

        /// <summary>
        /// Gets or sets an alternative parent reference for the message.
        /// </summary>
        public string? RelatedTo { get; set; }

        /// <summary>
        /// Gets or sets the full type name of the message.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Gets the short type name of the message.
        /// </summary>
        public string? MessageTypeShort => MessageType?.Split(',')[0].Split('.').Last();

        /// <summary>
        /// Gets or sets the name of the endpoint that sent or received the message.
        /// </summary>
        public string EndpointName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier of the host (machine or container) processing the message.
        /// </summary>
        public string HostId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the direction of the message (incoming or outgoing).
        /// </summary>
        public MessageDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the telemetry event occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the duration of message processing, if available.
        /// </summary>
        public TimeSpan? ProcessingDuration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message processing succeeded.
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// Gets or sets the type of exception thrown during message processing, if any.
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception message if message processing failed.
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets all NServiceBus headers associated with the message.
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Gets or sets the saga identifier if the message is related to a saga.
        /// </summary>
        public string? SagaId { get; set; }

        /// <summary>
        /// Gets or sets the type of saga if the message is related to a saga.
        /// </summary>
        public string? SagaType { get; set; }

        /// <summary>
        /// Gets or sets the originating endpoint where the message flow started.
        /// </summary>
        public string? OriginatingEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the reply-to address for the message, if available.
        /// </summary>
        public string? ReplyToAddress { get; set; }

        /// <summary>
        /// Gets or sets the retry attempt number for the message, if applicable.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// The intent of the message (Send, Publish, Reply)
        /// </summary>
        public MessageIntent Intent { get; set; } = MessageIntent.Unknown;
    }
}
