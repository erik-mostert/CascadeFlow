using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Cascade.Sample.BillingService.Handlers;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IHandleMessages<OrderPlaced>
{
    private readonly ILogger<OrderPlacedHandler> _logger = logger;

    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Billing received OrderPlaced for {OrderId}, charging {Amount}",
            message.OrderId, message.Amount);

        // Simulate payment processing
        await Task.Delay(100);

        await context.Publish(new OrderBilled
        {
            OrderId = message.OrderId,
            AmountCharged = message.Amount,
            BilledAt = DateTimeOffset.UtcNow
        });

        _logger.LogInformation("Published OrderBilled for {OrderId}", message.OrderId);
    }
}