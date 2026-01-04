using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Framework.Dispatchers;

namespace Cascade.NServiceBus.Framework.Behaviors
{
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
            string exceptionType = null;
            string exceptionMessage = null;

            // Get message intent
            string intentValue;
            var intentHeader = context.MessageHeaders.TryGetValue("NServiceBus.MessageIntent", out intentValue)
                ? intentValue
                : null;

            MessageIntent intent;
            switch (intentHeader)
            {
                case "Send":
                    intent = MessageIntent.Send;
                    break;
                case "Publish":
                    intent = MessageIntent.Publish;
                    break;
                case "Reply":
                    intent = MessageIntent.Reply;
                    break;
                default:
                    intent = MessageIntent.Unknown;
                    break;
            }

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

                int retryCount;
                var retryHeader = GetHeader(context, "NServiceBus.Retries");
                int? retryCountValue = int.TryParse(retryHeader, out retryCount) ? retryCount : (int?)null;

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
                    RetryCount = retryCountValue,
                    Headers = _options.IncludeHeaders
                        ? context.MessageHeaders.ToDictionary(h => h.Key, h => h.Value)
                        : null,
                    Intent = intent
                };

                // Fire and forget - never slow down message processing
                var _ = _dispatcher.DispatchAsync(telemetry, context.CancellationToken);
            }
        }

        private static string GetHeader(IIncomingPhysicalMessageContext context, string key)
        {
            string value;
            return context.MessageHeaders.TryGetValue(key, out value) ? value : null;
        }
    }
}
