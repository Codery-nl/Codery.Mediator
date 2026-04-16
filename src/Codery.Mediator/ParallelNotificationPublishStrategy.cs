namespace Codery.Mediator;

/// <summary>
/// Publishes notifications to handlers in parallel. All handlers are invoked even if
/// one or more throw an exception.
/// </summary>
public sealed class ParallelNotificationPublishStrategy : INotificationPublishStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = handlers
            .Select(handler =>
            {
                try
                {
                    return handler.Handle(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            })
            .ToList();

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Task.WhenAll throws only the first exception.
            // Collect all faulted task exceptions into an AggregateException.
            var exceptions = tasks
                .Where(t => t.IsFaulted)
                .SelectMany(t => t.Exception!.InnerExceptions)
                .ToList();

            throw new AggregateException(
                $"One or more notification handlers for {typeof(TNotification).Name} threw an exception.",
                exceptions);
        }
    }
}
