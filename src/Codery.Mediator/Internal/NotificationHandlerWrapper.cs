namespace Codery.Mediator.Internal;

/// <summary>
/// Abstract base that type-erases the TNotification generic parameter,
/// enabling dictionary-based caching keyed by notification type.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    /// <summary>
    /// Publishes a notification by resolving all handlers from the service provider.
    /// </summary>
    public abstract Task Handle(
        object notification,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
