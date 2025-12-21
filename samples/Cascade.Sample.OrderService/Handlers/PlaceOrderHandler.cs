using Cascade.Sample.Contracts.Commands;
using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Cascade.Sample.OrderService.Handlers;

public class PlaceOrderHandler(ILogger<PlaceOrderHandler> logger) : IHandleMessages<PlaceOrder>
{
  private readonly ILogger<PlaceOrderHandler> _logger = logger;

  public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
  {
    _logger.LogInformation("Received PlaceOrder for {OrderId}", message.OrderId);

    // Simulate some processing
    await Task.Delay(50, context.CancellationToken);

    // Publish event
    await context.Publish(new OrderPlaced
    {
      OrderId = message.OrderId,
      CustomerId = message.CustomerId,
      ProductName = message.ProductName,
      Amount = message.Amount,
      PlacedAt = DateTimeOffset.UtcNow
    });

    _logger.LogInformation("Published OrderPlaced for {OrderId}", message.OrderId);
  }
}