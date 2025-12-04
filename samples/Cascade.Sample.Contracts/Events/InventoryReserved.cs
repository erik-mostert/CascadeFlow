using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

public class InventoryReserved : IEvent
{
  public required string OrderId { get; init; }
  public required string ProductName { get; init; }
  public required DateTimeOffset ReservedAt { get; init; }
}