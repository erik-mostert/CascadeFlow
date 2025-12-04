using NServiceBus;

namespace Cascade.Sample.Contracts.Events;

public class PaymentFailed : IEvent
{
  public required Guid OrderId { get; init; }
  public required string Reason { get; init; }
  public required DateTimeOffset FailedAt { get; init; }
}