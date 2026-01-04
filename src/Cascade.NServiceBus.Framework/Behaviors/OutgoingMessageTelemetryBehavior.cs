using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Framework.Dispatchers;

namespace Cascade.NServiceBus.Framework.Behaviors
{
    /// <summary>
    /// Captures telemetry for outgoing messages (being sent/published).
    /// </summary>
    public class OutgoingMessageTelemetryBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        private readonly ITelemetryDispatcher _dispatcher;
        private readonly CascadeOptions _options;

        public OutgoingMessageTelemetryBehavior(ITelemetryDispatcher dispatcher, CascadeOptions options)
        {
            _dispatcher = dispatcher;
            _options = options;
        }

        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            // Get message intent
            string intentValue;
            var intentHeader = context.Headers.TryGetValue("NServiceBus.MessageIntent", out intentValue)
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
                Direction = MessageDirection.Outgoing,
                Timestamp = DateTimeOffset.UtcNow,
                Success = true,
                OriginatingEndpoint = GetHeader(context, "NServiceBus.OriginatingEndpoint"),
                Headers = _options.IncludeHeaders
                    ? context.Headers.ToDictionary(h => h.Key, h => h.Value)
                    : null,
                Intent = intent
            };

            // Fire and forget
            var _ = _dispatcher.DispatchAsync(telemetry, context.CancellationToken);
        }

        private static string GetHeader(IOutgoingPhysicalMessageContext context, string key)
        {
            string value;
            return context.Headers.TryGetValue(key, out value) ? value : null;
        }
    }
}
