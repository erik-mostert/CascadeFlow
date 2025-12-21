using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Cascade.Sample.Billing.Handlers;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IHandleMessages<OrderPlaced>
{
  private readonly ILogger<OrderPlacedHandler> _logger = logger;
  private static int _messageCount = 0;

  public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
  {
    _messageCount++;

    _logger.LogInformation("Billing received OrderPlaced for {OrderId}, charging {Amount}",
        message.OrderId, message.Amount);

    // Simulate payment processing
    await Task.Delay(100, context.CancellationToken);

    // Fail every 3rd message to demonstrate error handling
    if (_messageCount % 3 == 0)
    {
      _logger.LogWarning("Payment processing failed for {OrderId}!", message.OrderId);
      throw new InvalidOperationException($"Payment gateway timeout for order {message.OrderId}");
    }

    await context.Publish(new OrderBilled
    {
      OrderId = message.OrderId,
      AmountCharged = message.Amount,
      BilledAt = DateTimeOffset.UtcNow
    });

    _logger.LogInformation("Published OrderBilled for {OrderId}", message.OrderId);
  }
}