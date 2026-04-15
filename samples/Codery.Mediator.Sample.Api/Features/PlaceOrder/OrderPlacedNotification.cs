using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.PlaceOrder;

public sealed record OrderPlacedNotification(string OrderId, string ProductName) : INotification;

public sealed class OrderPlacedEmailHandler(ILogger<OrderPlacedEmailHandler> logger)
    : INotificationHandler<OrderPlacedNotification>
{
    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending email for order {OrderId}: {Product}",
            notification.OrderId, notification.ProductName);
        return Task.CompletedTask;
    }
}

public sealed class OrderPlacedLogHandler(ILogger<OrderPlacedLogHandler> logger)
    : INotificationHandler<OrderPlacedNotification>
{
    public Task Handle(OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order placed: {OrderId} for {Product}",
            notification.OrderId, notification.ProductName);
        return Task.CompletedTask;
    }
}
