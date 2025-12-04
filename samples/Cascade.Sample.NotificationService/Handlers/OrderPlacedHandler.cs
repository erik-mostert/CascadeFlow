using Cascade.Sample.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Cascade.Sample.NotificationService.Handlers;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IHandleMessages<OrderPlaced>
{
    private readonly ILogger<OrderPlacedHandler> _logger = logger;

    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Notifications received OrderPlaced for {OrderId}, notifying customer {CustomerId}",
            message.OrderId, message.CustomerId);

        // Simulate sending notification
        await Task.Delay(50);

        await context.Publish(new CustomerNotified
        {
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            NotificationType = "OrderConfirmation",
            NotifiedAt = DateTimeOffset.UtcNow
        });

        _logger.LogInformation("Published CustomerNotified for {OrderId}", message.OrderId);
    }
}