namespace Codery.Mediator;

/// <summary>
/// Represents the next action in the stream request pipeline. May invoke another behavior or the final handler.
/// </summary>
/// <typeparam name="TResponse">The type of items in the response stream.</typeparam>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();

/// <summary>
/// Pipeline behavior that wraps stream request handling with cross-cutting concerns.
/// </summary>
/// <typeparam name="TRequest">The type of stream request being handled.</typeparam>
/// <typeparam name="TResponse">The type of items in the response stream.</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the stream pipeline step. Call <paramref name="next"/> to continue the pipeline,
    /// or return a stream directly to short-circuit.
    /// </summary>
    /// <param name="request">The incoming stream request.</param>
    /// <param name="next">The delegate representing the next step in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of responses.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
