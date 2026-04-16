using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator.Internal;

/// <summary>
/// Internal pipeline behavior that invokes all registered <see cref="IRequestPostProcessor{TRequest, TResponse}"/>
/// after the handler. Registered as the innermost behavior to run right around the handler.
/// </summary>
internal sealed class RequestPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public RequestPostProcessorBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);

        var processors = _serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>();

        foreach (var processor in processors)
        {
            await processor.Process(request, response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
