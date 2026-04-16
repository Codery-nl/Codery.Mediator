namespace Codery.Mediator.Internal;

/// <summary>
/// Abstract base that type-erases the TRequest generic parameter for stream requests,
/// enabling dictionary-based caching keyed by stream request type.
/// </summary>
/// <typeparam name="TResponse">The response stream item type.</typeparam>
internal abstract class StreamRequestHandlerWrapper<TResponse>
{
    /// <summary>
    /// Handles a stream request by resolving its handler from the service provider.
    /// </summary>
    public abstract IAsyncEnumerable<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
