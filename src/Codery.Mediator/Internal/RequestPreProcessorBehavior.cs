using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator.Internal;

/// <summary>
/// Internal pipeline behavior that invokes all registered <see cref="IRequestPreProcessor{TRequest}"/>
/// before the handler. Registered as the outermost behavior to run first.
/// </summary>
internal sealed class RequestPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public RequestPreProcessorBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var processors = _serviceProvider.GetServices<IRequestPreProcessor<TRequest>>();

        foreach (var processor in processors)
        {
            await processor.Process(request, cancellationToken).ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}
