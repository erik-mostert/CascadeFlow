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

    // Publish multiple events from a single handler
    var shipmentId = Guid.NewGuid();

    // Event 1: Shipment scheduled
    await context.Publish(new ShipmentScheduled
    {
      OrderId = message.OrderId,
      ShipmentId = shipmentId,
      EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(3)
    });
    _logger.LogInformation("Published ShipmentScheduled for {OrderId}", message.OrderId);

    // Event 2: Inventory reserved (new event)
    await context.Publish(new InventoryReserved
    {
      OrderId = message.OrderId,
      ProductName = message.ProductName,
      ReservedAt = DateTimeOffset.UtcNow
    });
    _logger.LogInformation("Published InventoryReserved for {OrderId}", message.OrderId);

    // Event 3: Warehouse notified (new event)
    await context.Publish(new WarehouseNotified
    {
      OrderId = message.OrderId,
      ShipmentId = shipmentId,
      NotifiedAt = DateTimeOffset.UtcNow
    });
    _logger.LogInformation("Published WarehouseNotified for {OrderId}", message.OrderId);
  }
}