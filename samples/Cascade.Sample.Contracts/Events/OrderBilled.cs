using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

/// <summary>
/// Event published when an order has been billed.
/// </summary>
public class OrderBilled : IEvent
{
  public required string OrderId { get; init; }
  public required decimal AmountCharged { get; init; }
  public required DateTimeOffset BilledAt { get; init; }
}