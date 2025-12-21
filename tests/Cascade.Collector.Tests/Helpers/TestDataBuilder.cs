using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Collector.Tests.Helpers;

/// <summary>
/// Helper class for building test data objects.
/// </summary>
public static class TestDataBuilder
{
    private static int _counter = 0;

    /// <summary>
    /// Creates a MessageTelemetry with default values that can be overridden.
    /// </summary>
    public static MessageTelemetry CreateTelemetry(
        string? id = null,
        string? messageId = null,
        string? correlationId = null,
        string? messageType = null,
        string? endpointName = null,
        string? hostId = null,
        MessageDirection direction = MessageDirection.Incoming,
        DateTimeOffset? timestamp = null,
        TimeSpan? processingDuration = null,
        bool? success = true,
        string? originatingEndpoint = null,
        string? relatedTo = null,
        MessageIntent intent = MessageIntent.Unknown)
    {
        var counter = Interlocked.Increment(ref _counter);

        return new MessageTelemetry
        {
            Id = id ?? $"telemetry-{counter}",
            MessageId = messageId ?? $"msg-{counter}",
            CorrelationId = correlationId,
            MessageType = messageType ?? $"TestNamespace.TestMessage{counter}, TestAssembly",
            EndpointName = endpointName ?? $"TestEndpoint{counter}",
            HostId = hostId ?? "test-host",
            Direction = direction,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            ProcessingDuration = processingDuration,
            Success = success,
            OriginatingEndpoint = originatingEndpoint,
            RelatedTo = relatedTo,
            Intent = intent
        };
    }

    /// <summary>
    /// Creates an incoming message telemetry.
    /// </summary>
    public static MessageTelemetry CreateIncomingMessage(
        string? correlationId = null,
        string? endpointName = null,
        string? messageType = null,
        bool success = true,
        TimeSpan? processingDuration = null,
        string? originatingEndpoint = null)
    {
        return CreateTelemetry(
            correlationId: correlationId,
            endpointName: endpointName,
            messageType: messageType,
            direction: MessageDirection.Incoming,
            success: success,
            processingDuration: processingDuration ?? TimeSpan.FromMilliseconds(100),
            originatingEndpoint: originatingEndpoint);
    }

    /// <summary>
    /// Creates an outgoing message telemetry.
    /// </summary>
    public static MessageTelemetry CreateOutgoingMessage(
        string? correlationId = null,
        string? endpointName = null,
        string? messageType = null,
        string? relatedTo = null,
        MessageIntent intent = MessageIntent.Send)
    {
        return CreateTelemetry(
            correlationId: correlationId,
            endpointName: endpointName,
            messageType: messageType,
            direction: MessageDirection.Outgoing,
            success: true,
            relatedTo: relatedTo,
            intent: intent);
    }

    /// <summary>
    /// Creates a failed message telemetry.
    /// </summary>
    public static MessageTelemetry CreateFailedMessage(
        string? correlationId = null,
        string? endpointName = null,
        string? exceptionType = null,
        string? exceptionMessage = null)
    {
        var counter = Interlocked.Increment(ref _counter);

        return new MessageTelemetry
        {
            Id = $"telemetry-{counter}",
            MessageId = $"msg-{counter}",
            CorrelationId = correlationId,
            MessageType = $"TestNamespace.FailedMessage{counter}, TestAssembly",
            EndpointName = endpointName ?? "FailingEndpoint",
            HostId = "test-host",
            Direction = MessageDirection.Incoming,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessingDuration = TimeSpan.FromMilliseconds(50),
            Success = false,
            ExceptionType = exceptionType ?? "System.InvalidOperationException",
            ExceptionMessage = exceptionMessage ?? "Test exception message"
        };
    }

    /// <summary>
    /// Creates a MessageFlow with the specified messages.
    /// </summary>
    public static MessageFlow CreateFlow(
        string? correlationId = null,
        DateTimeOffset? startedAt = null,
        FlowStatus status = FlowStatus.InProgress,
        params MessageTelemetry[] messages)
    {
        var counter = Interlocked.Increment(ref _counter);
        var flowCorrelationId = correlationId ?? $"correlation-{counter}";

        return new MessageFlow
        {
            CorrelationId = flowCorrelationId,
            StartedAt = startedAt ?? DateTimeOffset.UtcNow,
            Status = status,
            Messages = messages.Length > 0 ? [.. messages] : []
        };
    }

    /// <summary>
    /// Resets the counter for test isolation.
    /// </summary>
    public static void ResetCounter()
    {
        Interlocked.Exchange(ref _counter, 0);
    }
}
