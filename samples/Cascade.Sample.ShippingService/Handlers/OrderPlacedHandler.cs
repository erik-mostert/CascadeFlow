using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Cascade.Sample.ShippingService.Handlers;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IHandleMessages<OrderPlaced>
{
  private readonly ILogger<OrderPlacedHandler> _logger = logger;

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