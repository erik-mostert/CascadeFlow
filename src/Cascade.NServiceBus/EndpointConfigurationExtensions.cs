using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;

namespace Cascade.NServiceBus;

/// <summary>
/// Extension methods for explicit Cascade configuration.
/// Use this if you want to override the default auto-configuration.
/// </summary>
public static class EndpointConfigurationExtensions
{
  /// <summary>
  /// Explicitly configures Cascade telemetry with custom options.
  /// This overrides the automatic environment variable configuration.
  /// </summary>
  public static void UseCascade(this EndpointConfiguration configuration, Action<CascadeOptions> configure)
  {
    var settings = configuration.GetSettings();

    // Mark as configured to prevent INeedInitialization from running
    settings.Set("Cascade.Configured", true);

    var options = new CascadeOptions
    {
      EndpointName = settings.EndpointName()
    };

    configure(options);

    CascadeInitializer.RegisterBehaviors(configuration, options);

    Console.WriteLine($"[Cascade] Telemetry configured -> {options.CollectorUrl}");
  }

  /// <summary>
  /// Disables Cascade telemetry for this endpoint.
  /// </summary>
  public static void DisableCascade(this EndpointConfiguration configuration)
  {
    configuration.GetSettings().Set("Cascade.Configured", true);
    Console.WriteLine("[Cascade] Telemetry explicitly disabled");
  }
}