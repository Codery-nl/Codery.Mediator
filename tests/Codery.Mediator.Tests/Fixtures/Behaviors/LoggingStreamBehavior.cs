namespace Codery.Mediator.Tests.Fixtures.Behaviors;

/// <summary>
/// Open generic stream behavior used only for type reference in MediatorOptions tests.
/// </summary>
public sealed class LoggingStreamBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}
