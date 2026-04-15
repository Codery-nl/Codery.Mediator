namespace Codery.Mediator.Tests.Fixtures.Behaviors;

/// <summary>
/// Open generic behavior used only for type reference in MediatorOptions tests.
/// Tests that need actual behavior execution use local TrackingBehavior instances.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}
