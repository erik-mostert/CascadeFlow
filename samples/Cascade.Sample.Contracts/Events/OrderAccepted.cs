using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

/// <summary>
/// Event published when an order has been accepted for processing.
/// </summary>
public class OrderAccepted : IEvent
{
  public required string OrderId { get; init; }
  public required DateTimeOffset AcceptedAt { get; init; }
}