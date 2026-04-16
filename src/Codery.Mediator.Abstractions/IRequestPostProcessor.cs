namespace Codery.Mediator;

/// <summary>
/// Defines a post-processor that runs after the request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Processes the request after the handler has executed.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="response">The response from the handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
