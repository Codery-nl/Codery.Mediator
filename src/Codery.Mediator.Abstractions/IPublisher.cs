namespace Codery.Mediator;

/// <summary>
/// Publishes a notification to all registered handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers sequentially.
    /// All handlers are invoked even if one or more throw an exception.
    /// If any handlers throw, an <see cref="AggregateException"/> is thrown after all handlers have been invoked.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="AggregateException">Thrown when one or more handlers throw an exception.</exception>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
