using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace Cascade.Sample.Shipping.Handlers;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
  private readonly ILogger<OrderPlacedHandler> _logger;

  public OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
  {
    _logger = logger;
  }

  public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
  {
    _logger.LogInformation("Shipping received OrderPlaced for {OrderId}", message.OrderId);

    // Simulate processing
    await Task.Delay(75);

    // Publish shipment scheduled event
    await context.Publish(new ShipmentScheduled
    {
      OrderId = message.OrderId,
      ShipmentId = $"SHIP-{Guid.NewGuid().ToString()[..8]}",
      EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(3)
    });

    _logger.LogInformation("Published ShipmentScheduled for {OrderId}", message.OrderId);
  }
}