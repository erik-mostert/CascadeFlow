namespace Cascade.Core.Models;

/// <summary>
/// A message type observed in the system.
/// </summary>
public record TopologyMessageType
{
  /// <summary>Full assembly-qualified type name.</summary>
  public required string FullName { get; init; }

  /// <summary>Short type name (class name only).</summary>
  public string ShortName => FullName.Split(',')[0].Split('.').LastOrDefault() ?? FullName;

  /// <summary>Number of times this message type has been observed.</summary>
  public long TimesObserved { get; set; }

  /// <summary>When this message type was first observed.</summary>
  public DateTimeOffset FirstSeen { get; set; }

  /// <summary>When this message type was last observed.</summary>
  public DateTimeOffset LastSeen { get; set; }
}