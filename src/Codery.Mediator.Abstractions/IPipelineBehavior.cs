namespace Codery.Mediator;

/// <summary>
/// Represents the next action in the request pipeline. May invoke another behavior or the final handler.
/// </summary>
/// <typeparam name="TResponse">The type of response from the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Pipeline behavior that wraps request handling with cross-cutting concerns
/// such as logging, validation, or transaction management.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the pipeline step. Call <paramref name="next"/> to continue the pipeline,
    /// or return a response directly to short-circuit.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The delegate representing the next step in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the pipeline.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
