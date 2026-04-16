using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator.Internal;

/// <summary>
/// Concrete notification wrapper that is fully generic over TNotification.
/// One instance is created per notification type and cached for the application lifetime.
/// </summary>
internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    /// <inheritdoc />
    public override Task Handle(
        object notification,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Debug.Assert(notification is TNotification, $"Expected {typeof(TNotification).Name}, got {notification.GetType().Name}");
        var typedNotification = (TNotification)notification;
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();
        var strategy = serviceProvider.GetRequiredService<INotificationPublishStrategy>();

        return strategy.PublishAsync(handlers, typedNotification, cancellationToken);
    }
}
