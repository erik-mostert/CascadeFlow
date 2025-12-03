using NServiceBus;

namespace Cascade.Sample.Contracts.Commands;

/// <summary>
/// Command to place a new order.
/// </summary>
public class PlaceOrder : ICommand
{
  public required string OrderId { get; init; }
  public required string CustomerId { get; init; }
  public required string ProductName { get; init; }
  public required decimal Amount { get; init; }
}