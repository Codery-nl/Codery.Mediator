namespace Codery.Mediator;

/// <summary>
/// Defines a strategy for publishing notifications to multiple handlers.
/// </summary>
public interface INotificationPublishStrategy
{
    /// <summary>
    /// Publishes a notification to the specified handlers.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="handlers">The notification handlers to invoke.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification;
}
