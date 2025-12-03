using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

/// <summary>
/// Event published when the customer has been notified about their order.
/// </summary>
public class CustomerNotified : IEvent
{
  public required string OrderId { get; init; }
  public required string CustomerId { get; init; }
  public required string NotificationType { get; init; }
  public required DateTimeOffset NotifiedAt { get; init; }
}