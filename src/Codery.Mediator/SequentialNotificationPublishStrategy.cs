namespace Codery.Mediator;

/// <summary>
/// Publishes notifications to handlers sequentially. All handlers are invoked even if
/// one or more throw an exception. This is the default strategy.
/// </summary>
public sealed class SequentialNotificationPublishStrategy : INotificationPublishStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
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
