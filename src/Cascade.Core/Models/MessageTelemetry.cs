using Cascade.Core.Enums;

namespace Cascade.Core.Models;

/// <summary>
/// Represents telemetry data for an NServiceBus message event.
/// </summary>
public record MessageTelemetry
{
  /// <summary>
  /// Gets the unique identifier for the telemetry event.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// Gets the unique identifier assigned to the message by NServiceBus.
  /// </summary>
  /// <remarks>The message ID is used to track and correlate messages within the messaging infrastructure. This
  /// value is set by NServiceBus when the message is sent and remains unchanged throughout the message's
  /// lifecycle.</remarks>
  public required string MessageId { get; init; }

  /// <summary>
  /// Gets the correlation identifier for linking related messages.
  /// </summary>
  public string? CorrelationId { get; init; }

  /// <summary>
  /// Gets the unique identifier for the conversation associated with the message.
  /// </summary>
  /// <remarks>This identifier is used to correlate related messages within a single logical conversation in
  /// NServiceBus. The value may be null if the message is not part of a conversation.</remarks>
  public string? ConversationId { get; init; }

  /// <summary>
  /// Gets the causation identifier, which references the direct parent message.
  /// </summary>
  public string? CausationId { get; init; }

  /// <summary>
  /// Gets an alternative parent reference for the message.
  /// </summary>
  public string? RelatedTo { get; init; }

  /// <summary>
  /// Gets the full type name of the message.
  /// </summary>
  public required string MessageType { get; init; }

  /// <summary>
  /// Gets the short type name of the message.
  /// </summary>
  public string? MessageTypeShort => MessageType?.Split(',')[0].Split('.').Last();

  /// <summary>
  /// Gets the name of the endpoint that sent or received the message.
  /// </summary>
  public required string EndpointName { get; init; }

  /// <summary>
  /// Gets the unique identifier of the host (machine or container) processing the message.
  /// </summary>
  public required string HostId { get; init; }

  /// <summary>
  /// Gets the direction of the message (incoming or outgoing).
  /// </summary>
  public required MessageDirection Direction { get; init; }

  /// <summary>
  /// Gets the timestamp when the telemetry event occurred.
  /// </summary>
  public required DateTimeOffset Timestamp { get; init; }

  /// <summary>
  /// Gets the duration of message processing, if available.
  /// </summary>
  public TimeSpan? ProcessingDuration { get; init; }

  /// <summary>
  /// Gets a value indicating whether the message processing succeeded.
  /// </summary>
  public bool? Success { get; init; }

  /// <summary>
  /// Gets the type of exception thrown during message processing, if any.
  /// </summary>
  public string? ExceptionType { get; init; }

  /// <summary>
  /// Gets the exception message if message processing failed.
  /// </summary>
  public string? ExceptionMessage { get; init; }

  /// <summary>
  /// Gets all NServiceBus headers associated with the message.
  /// </summary>
  public Dictionary<string, string>? Headers { get; init; }

  /// <summary>
  /// Gets the saga identifier if the message is related to a saga.
  /// </summary>
  public string? SagaId { get; init; }

  /// <summary>
  /// Gets the type of saga if the message is related to a saga.
  /// </summary>
  public string? SagaType { get; init; }

  /// <summary>
  /// Gets the originating endpoint where the message flow started.
  /// </summary>
  public string? OriginatingEndpoint { get; init; }

  /// <summary>
  /// Gets the reply-to address for the message, if available.
  /// </summary>
  public string? ReplyToAddress { get; init; }

  /// <summary>
  /// Gets the retry attempt number for the message, if applicable.
  /// </summary>
  public int? RetryCount { get; init; }

  /// <summary>
  /// The intent of the message (Send, Publish, Reply)
  /// </summary>
  public MessageIntent Intent { get; set; } = MessageIntent.Unknown;
}
