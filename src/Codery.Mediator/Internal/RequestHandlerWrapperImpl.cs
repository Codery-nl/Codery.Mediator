using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator.Internal;

/// <summary>
/// Concrete handler wrapper that is fully generic over TRequest and TResponse.
/// One instance is created per request type and cached for the application lifetime.
/// All operations are strongly-typed with zero reflection on the hot path.
/// </summary>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public override async Task<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Safe by construction: the static cache keys by request.GetType(),
        // and this wrapper is only instantiated for matching TRequest/TResponse pairs.
        Debug.Assert(request is TRequest, $"Expected {typeof(TRequest).Name}, got {request.GetType().Name}");
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        // Innermost delegate: the actual handler
        RequestHandlerDelegate<TResponse> pipeline = () =>
            handler.Handle(typedRequest, cancellationToken);

        // Wrap behaviors in reverse order so first-registered runs outermost (first).
        // Materialize once and iterate backwards to avoid Reverse() allocation.
        var behaviorList = behaviors as IList<IPipelineBehavior<TRequest, TResponse>> ?? behaviors.ToList();
        for (int i = behaviorList.Count - 1; i >= 0; i--)
        {
            var behavior = behaviorList[i];
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }
}
