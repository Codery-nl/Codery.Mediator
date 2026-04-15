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
    public override async Task Handle(
        object notification,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Debug.Assert(notification is TNotification, $"Expected {typeof(TNotification).Name}, got {notification.GetType().Name}");
        var typedNotification = (TNotification)notification;
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(typedNotification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(
                $"One or more notification handlers for {typeof(TNotification).Name} threw an exception.",
                exceptions);
        }
    }
}
