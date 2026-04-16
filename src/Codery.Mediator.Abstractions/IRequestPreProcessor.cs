namespace Codery.Mediator;

/// <summary>
/// Defines a pre-processor that runs before the request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
public interface IRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Processes the request before the handler executes.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, CancellationToken cancellationToken);
}
