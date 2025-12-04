using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

public class WarehouseNotified : IEvent
{
  public required string OrderId { get; init; }
  public required Guid ShipmentId { get; init; }
  public required DateTimeOffset NotifiedAt { get; init; }
}