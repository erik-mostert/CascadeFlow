using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

/// <summary>
/// Event published when an order has been placed.
/// </summary>
public class OrderPlaced : IEvent
{
  public required string OrderId { get; init; }
  public required string CustomerId { get; init; }
  public required string ProductName { get; init; }
  public required decimal Amount { get; init; }
  public required DateTimeOffset PlacedAt { get; init; }
}