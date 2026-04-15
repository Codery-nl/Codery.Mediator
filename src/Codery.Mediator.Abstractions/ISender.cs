namespace Codery.Mediator;

/// <summary>
/// Sends a request to a single handler and returns the response.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request to its corresponding handler.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The handler's response.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
