namespace Codery.Mediator.Internal;

/// <summary>
/// Abstract base that type-erases the TRequest generic parameter,
/// enabling dictionary-based caching keyed by request type.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
internal abstract class RequestHandlerWrapper<TResponse>
{
    /// <summary>
    /// Handles a request by resolving its handler from the service provider.
    /// </summary>
    public abstract Task<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
