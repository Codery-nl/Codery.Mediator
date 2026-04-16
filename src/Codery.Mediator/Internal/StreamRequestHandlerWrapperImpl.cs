using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator.Internal;

/// <summary>
/// Concrete stream handler wrapper that is fully generic over TRequest and TResponse.
/// One instance is created per stream request type and cached for the application lifetime.
/// All operations are strongly-typed with zero reflection on the hot path.
/// </summary>
internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <inheritdoc />
    public override IAsyncEnumerable<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Safe by construction: the static cache keys by request.GetType(),
        // and this wrapper is only instantiated for matching TRequest/TResponse pairs.
        Debug.Assert(request is TRequest, $"Expected {typeof(TRequest).Name}, got {request.GetType().Name}");
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IStreamPipelineBehavior<TRequest, TResponse>>();

        // Innermost delegate: the actual handler
        StreamHandlerDelegate<TResponse> pipeline = () =>
            handler.Handle(typedRequest, cancellationToken);

        // Wrap behaviors in reverse order so first-registered runs outermost (first).
        // Materialize once and iterate backwards to avoid Reverse() allocation.
        var behaviorList = behaviors as IList<IStreamPipelineBehavior<TRequest, TResponse>> ?? behaviors.ToList();
        for (int i = behaviorList.Count - 1; i >= 0; i--)
        {
            var behavior = behaviorList[i];
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
