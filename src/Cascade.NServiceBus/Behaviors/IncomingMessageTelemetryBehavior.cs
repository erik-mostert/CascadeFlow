using System.Diagnostics;
using NServiceBus.Pipeline;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Dispatchers;
using MessageIntent = Cascade.Core.Enums.MessageIntent;

namespace Cascade.NServiceBus.Behaviors;

/// <summary>
/// Captures telemetry for incoming messages (being handled).
/// </summary>
public class IncomingMessageTelemetryBehavior : Behavior<IIncomingPhysicalMessageContext>
{
  private readonly ITelemetryDispatcher _dispatcher;
  private readonly CascadeOptions _options;

  public IncomingMessageTelemetryBehavior(ITelemetryDispatcher dispatcher, CascadeOptions options)
  {
    _dispatcher = dispatcher;
    _options = options;
  }

  public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
  {
    var stopwatch = Stopwatch.StartNew();
    var success = true;
    string? exceptionType = null;
    string? exceptionMessage = null;
    // Get message intent
    var intentHeader = context.MessageHeaders.TryGetValue("NServiceBus.MessageIntent", out var intentValue)
        ? intentValue
        : null;

    var intent = intentHeader switch
    {
      "Send" => MessageIntent.Send,
      "Publish" => MessageIntent.Publish,
      "Reply" => MessageIntent.Reply,
      _ => MessageIntent.Unknown
    };

    try
    {
      await next().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      success = false;
      exceptionType = ex.GetType().FullName;
      exceptionMessage = ex.Message;
      throw;
    }
    finally
    {
      stopwatch.Stop();

      var telemetry = new MessageTelemetry
      {
        Id = Guid.NewGuid().ToString(),
        MessageId = context.MessageId,
        CorrelationId = GetHeader(context, "NServiceBus.CorrelationId"),
        ConversationId = GetHeader(context, "NServiceBus.ConversationId"),
        CausationId = GetHeader(context, "NServiceBus.CausationId"),
        RelatedTo = GetHeader(context, "NServiceBus.RelatedTo"),
        MessageType = GetHeader(context, "NServiceBus.EnclosedMessageTypes") ?? "Unknown",
        EndpointName = _options.EndpointName ?? "Unknown",
        HostId = _options.HostId ?? Environment.MachineName,
        Direction = MessageDirection.Incoming,
        Timestamp = DateTimeOffset.UtcNow,
        ProcessingDuration = stopwatch.Elapsed,
        Success = success,
        ExceptionType = exceptionType,
        ExceptionMessage = exceptionMessage,
        SagaId = GetHeader(context, "NServiceBus.SagaId"),
        SagaType = GetHeader(context, "NServiceBus.SagaType"),
        OriginatingEndpoint = GetHeader(context, "NServiceBus.OriginatingEndpoint"),
        ReplyToAddress = GetHeader(context, "NServiceBus.ReplyToAddress"),
        RetryCount = int.TryParse(GetHeader(context, "NServiceBus.Retries"), out var r) ? r : null,
        Headers = _options.IncludeHeaders
              ? context.MessageHeaders.ToDictionary(h => h.Key, h => h.Value)
              : null,
        Intent = intent
      };

      // Fire and forget - never slow down message processing
      _ = _dispatcher.DispatchAsync(telemetry, context.CancellationToken);
    }
  }

  private static string? GetHeader(IIncomingPhysicalMessageContext context, string key)
  {
    return context.MessageHeaders.TryGetValue(key, out var value) ? value : null;
  }
}