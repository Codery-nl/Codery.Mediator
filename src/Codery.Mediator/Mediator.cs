using System.Collections.Concurrent;
using Codery.Mediator.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Codery.Mediator;

/// <summary>
/// Default mediator implementation that dispatches requests, stream requests, and notifications
/// through the dependency injection container. Uses cached generic wrappers
/// to eliminate per-call reflection overhead.
/// </summary>
internal sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<Type, object> _requestWrapperFactory;

    // Static caches: shared across all Mediator instances for the application lifetime.
    // Key = concrete request/notification type. Value = cached wrapper instance.
    // Using object for request wrappers because RequestHandlerWrapper<TResponse> is generic.
    private static readonly ConcurrentDictionary<Type, object> RequestHandlerWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlerWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> StreamRequestHandlerWrappers = new();

    /// <summary>
    /// Clears the static wrapper caches. Intended for test isolation only.
    /// </summary>
    internal static void ClearCaches()
    {
        RequestHandlerWrappers.Clear();
        NotificationHandlerWrappers.Clear();
        StreamRequestHandlerWrappers.Clear();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;

        var config = serviceProvider.GetService<MediatorConfiguration>();
        var polymorphicEnabled = config?.PolymorphicDispatchEnabled ?? false;

        _requestWrapperFactory = polymorphicEnabled
            ? type => CreateWrapperWithPolymorphicFallback(type, serviceProvider)
            : static type => CreateStandardWrapper(type);
    }

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestHandlerWrappers.GetOrAdd(
            requestType,
            _requestWrapperFactory);

        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = (StreamRequestHandlerWrapper<TResponse>)StreamRequestHandlerWrappers.GetOrAdd(
            requestType,
            static type =>
            {
                // Runs once per stream request type. Extract TResponse from IStreamRequest<TResponse>.
                var requestInterface = type
                    .GetInterfaces()
                    .First(i => i.IsGenericType
                             && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>));

                var responseType = requestInterface.GetGenericArguments()[0];
                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(type, responseType);

                return Activator.CreateInstance(wrapperType)!;
            });

        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ThrowHelper.ThrowIfNull(notification);

        var notificationType = notification.GetType();

        var wrapper = NotificationHandlerWrappers.GetOrAdd(
            notificationType,
            static type =>
            {
                var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(type);
                return (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType)!;
            });

        return wrapper.Handle(notification, _serviceProvider, cancellationToken);
    }

    private static object CreateStandardWrapper(Type requestType)
    {
        // Runs once per request type. Extract TResponse from IRequest<TResponse>.
        var requestInterface = requestType
            .GetInterfaces()
            .First(i => i.IsGenericType
                     && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        var responseType = requestInterface.GetGenericArguments()[0];
        var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);

        return Activator.CreateInstance(wrapperType)!;
    }

    private static object CreateWrapperWithPolymorphicFallback(Type requestType, IServiceProvider serviceProvider)
    {
        // Walk the type hierarchy starting from the exact type.
        // Use the first type that has a registered handler.
        var currentType = requestType;

        while (currentType is not null)
        {
            var requestInterface = currentType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                                  && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestInterface is not null)
            {
                var responseType = requestInterface.GetGenericArguments()[0];
                var handlerType = typeof(IRequestHandler<,>).MakeGenericType(currentType, responseType);

                // Probe DI to see if a handler exists for this type.
                var handler = serviceProvider.GetService(handlerType);
                if (handler is not null)
                {
                    var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(currentType, responseType);
                    return Activator.CreateInstance(wrapperType)!;
                }
            }

            currentType = currentType.BaseType;
        }

        // No handler found at any level — fall back to exact type (will throw via GetRequiredService).
        return CreateStandardWrapper(requestType);
    }
}
