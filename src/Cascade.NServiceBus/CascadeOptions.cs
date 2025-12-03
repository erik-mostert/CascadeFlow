namespace Cascade.NServiceBus;

/// <summary>
/// Configuration options for Cascade telemetry collection.
/// </summary>
public class CascadeOptions
{
  /// <summary>
  /// URL of the Cascade collector. Default: http://localhost:5100
  /// </summary>
  public string CollectorUrl { get; set; } = "http://localhost:5100";

  /// <summary>
  /// Name of this endpoint. Auto-detected if not specified.
  /// </summary>
  public string? EndpointName { get; set; }

  /// <summary>
  /// Identifier for this host/instance. Defaults to machine name.
  /// </summary>
  public string? HostId { get; set; }

  /// <summary>
  /// Whether to include all message headers in telemetry. Default: true
  /// </summary>
  public bool IncludeHeaders { get; set; } = true;

  /// <summary>
  /// Maximum number of telemetry events to buffer before dropping. Default: 1000
  /// </summary>
  public int BufferSize { get; set; } = 1000;
}