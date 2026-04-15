using System.Collections.Concurrent;
using Codery.Mediator.Internal;

namespace Codery.Mediator;

/// <summary>
/// Default mediator implementation that dispatches requests and notifications
/// through the dependency injection container. Uses cached generic wrappers
/// to eliminate per-call reflection overhead.
/// </summary>
internal sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    // Static caches: shared across all Mediator instances for the application lifetime.
    // Key = concrete request/notification type. Value = cached wrapper instance.
    // Using object for request wrappers because RequestHandlerWrapper<TResponse> is generic.
    private static readonly ConcurrentDictionary<Type, object> RequestHandlerWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlerWrappers = new();

    /// <summary>
    /// Clears the static wrapper caches. Intended for test isolation only.
    /// </summary>
    internal static void ClearCaches()
    {
        RequestHandlerWrappers.Clear();
        NotificationHandlerWrappers.Clear();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        ThrowHelper.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
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
            static type =>
            {
                // Runs once per request type. Extract TResponse from IRequest<TResponse>.
                var requestInterface = type
                    .GetInterfaces()
                    .First(i => i.IsGenericType
                             && i.GetGenericTypeDefinition() == typeof(IRequest<>));

                var responseType = requestInterface.GetGenericArguments()[0];
                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(type, responseType);

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
}
