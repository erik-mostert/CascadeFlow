using System;
using System.Net.Http;
using Cascade.NServiceBus.Framework.Behaviors;
using Cascade.NServiceBus.Framework.Dispatchers;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

namespace Cascade.NServiceBus.Framework
{
    /// <summary>
    /// Automatically registers Cascade telemetry behaviors when the NuGet package is installed.
    /// No code changes required in the consuming endpoint.
    /// </summary>
    public class CascadeInitializer : INeedInitialization
    {
        public void Customize(EndpointConfiguration configuration)
        {
            // Check if explicitly disabled
            var enabledValue = Environment.GetEnvironmentVariable("CASCADE_ENABLED") ?? "true";
            bool isEnabled;
            if (!bool.TryParse(enabledValue, out isEnabled) || !isEnabled)
            {
                Console.WriteLine("[Cascade] Telemetry disabled via CASCADE_ENABLED=false");
                return;
            }

            // Check if already explicitly configured (don't double-register)
            var settings = configuration.GetSettings();
            if (settings.HasSetting("Cascade.Configured"))
            {
                return;
            }

            var options = new CascadeOptions
            {
                CollectorUrl = Environment.GetEnvironmentVariable("CASCADE_COLLECTOR_URL")
                    ?? "http://localhost:5100",
                EndpointName = settings.EndpointName(),
                HostId = Environment.GetEnvironmentVariable("CASCADE_HOST_ID")
                    ?? Environment.MachineName,
                ApiKey = Environment.GetEnvironmentVariable("CASCADE_API_KEY")
            };

            RegisterBehaviors(configuration, options);

            Console.WriteLine("[Cascade] Telemetry enabled -> " + options.CollectorUrl);
        }

        internal static void RegisterBehaviors(EndpointConfiguration configuration, CascadeOptions options)
        {
            var httpClient = new HttpClient();
            var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

            configuration.Pipeline.Register(
                new IncomingMessageTelemetryBehavior(dispatcher, options),
                "Cascade: Captures incoming message telemetry");

            configuration.Pipeline.Register(
                new OutgoingMessageTelemetryBehavior(dispatcher, options),
                "Cascade: Captures outgoing message telemetry");

            configuration.GetSettings().Set("Cascade.Configured", true);
        }
    }
}
