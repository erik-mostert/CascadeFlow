using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

/// <summary>
/// Event published when shipment has been scheduled for an order.
/// </summary>
public class ShipmentScheduled : IEvent
{
  public required string OrderId { get; init; }
  public required Guid ShipmentId { get; init; }
  public required DateTimeOffset EstimatedDelivery { get; init; }
}