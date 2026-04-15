namespace Codery.Mediator.Tests.Fixtures.Behaviors;

/// <summary>
/// A behavior that short-circuits the pipeline by returning a response without calling next().
/// </summary>
public sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TResponse _response;

    public ShortCircuitBehavior(TResponse response)
    {
        _response = response;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Deliberately does NOT call next()
        return Task.FromResult(_response);
    }
}
