namespace Codery.Mediator;

/// <summary>
/// Configuration options for the mediator pipeline.
/// </summary>
public sealed class MediatorOptions
{
    private readonly List<Type> _behaviorTypes = [];
    private readonly List<Type> _streamBehaviorTypes = [];
    private readonly List<Type> _preProcessorTypes = [];
    private readonly List<Type> _postProcessorTypes = [];

    internal IReadOnlyList<Type> BehaviorTypes => _behaviorTypes;
    internal IReadOnlyList<Type> StreamBehaviorTypes => _streamBehaviorTypes;
    internal IReadOnlyList<Type> PreProcessorTypes => _preProcessorTypes;
    internal IReadOnlyList<Type> PostProcessorTypes => _postProcessorTypes;
    internal bool PolymorphicDispatchEnabled { get; private set; }
    internal Type? NotificationPublishStrategyType { get; private set; }

    /// <summary>
    /// Registers an open generic pipeline behavior type.
    /// Behaviors execute in the order they are registered.
    /// </summary>
    /// <param name="openBehaviorType">
    /// An open generic type implementing <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// For example: <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// </param>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="openBehaviorType"/> is null.</exception>
    /// <exception cref="ArgumentException">When the type is not an open generic or does not implement <see cref="IPipelineBehavior{TRequest,TResponse}"/>.</exception>
    public MediatorOptions AddOpenBehavior(Type openBehaviorType)
    {
        Internal.ThrowHelper.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' must be an open generic type definition (e.g., typeof(MyBehavior<,>)).",
                nameof(openBehaviorType));
        }

        var implementsInterface = openBehaviorType
            .GetInterfaces()
            .Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!implementsInterface)
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' does not implement {nameof(IPipelineBehavior<IRequest<object>, object>)}<TRequest, TResponse>.",
                nameof(openBehaviorType));
        }

        if (!_behaviorTypes.Contains(openBehaviorType))
        {
            _behaviorTypes.Add(openBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Registers an open generic stream pipeline behavior type.
    /// Behaviors execute in the order they are registered.
    /// </summary>
    /// <param name="openBehaviorType">
    /// An open generic type implementing <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/>.
    /// For example: <c>typeof(LoggingStreamBehavior&lt;,&gt;)</c>.
    /// </param>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="openBehaviorType"/> is null.</exception>
    /// <exception cref="ArgumentException">When the type is not an open generic or does not implement <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/>.</exception>
    public MediatorOptions AddOpenStreamBehavior(Type openBehaviorType)
    {
        Internal.ThrowHelper.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' must be an open generic type definition (e.g., typeof(MyBehavior<,>)).",
                nameof(openBehaviorType));
        }

        var implementsInterface = openBehaviorType
            .GetInterfaces()
            .Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>));

        if (!implementsInterface)
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' does not implement {nameof(IStreamPipelineBehavior<IStreamRequest<object>, object>)}<TRequest, TResponse>.",
                nameof(openBehaviorType));
        }

        if (!_streamBehaviorTypes.Contains(openBehaviorType))
        {
            _streamBehaviorTypes.Add(openBehaviorType);
        }

        return this;
    }

    /// <summary>
    /// Registers an open generic request pre-processor type.
    /// Pre-processors run before the handler, in the order they are registered.
    /// </summary>
    /// <param name="openProcessorType">
    /// An open generic type implementing <see cref="IRequestPreProcessor{TRequest}"/>.
    /// For example: <c>typeof(ValidationPreProcessor&lt;&gt;)</c>.
    /// </param>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="openProcessorType"/> is null.</exception>
    /// <exception cref="ArgumentException">When the type is not an open generic or does not implement <see cref="IRequestPreProcessor{TRequest}"/>.</exception>
    public MediatorOptions AddOpenPreProcessor(Type openProcessorType)
    {
        Internal.ThrowHelper.ThrowIfNull(openProcessorType);

        if (!openProcessorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Type '{openProcessorType.FullName}' must be an open generic type definition (e.g., typeof(MyProcessor<>)).",
                nameof(openProcessorType));
        }

        var implementsInterface = openProcessorType
            .GetInterfaces()
            .Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>));

        if (!implementsInterface)
        {
            throw new ArgumentException(
                $"Type '{openProcessorType.FullName}' does not implement {nameof(IRequestPreProcessor<object>)}<TRequest>.",
                nameof(openProcessorType));
        }

        if (!_preProcessorTypes.Contains(openProcessorType))
        {
            _preProcessorTypes.Add(openProcessorType);
        }

        return this;
    }

    /// <summary>
    /// Registers an open generic request post-processor type.
    /// Post-processors run after the handler, in the order they are registered.
    /// </summary>
    /// <param name="openProcessorType">
    /// An open generic type implementing <see cref="IRequestPostProcessor{TRequest,TResponse}"/>.
    /// For example: <c>typeof(CachingPostProcessor&lt;,&gt;)</c>.
    /// </param>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="openProcessorType"/> is null.</exception>
    /// <exception cref="ArgumentException">When the type is not an open generic or does not implement <see cref="IRequestPostProcessor{TRequest,TResponse}"/>.</exception>
    public MediatorOptions AddOpenPostProcessor(Type openProcessorType)
    {
        Internal.ThrowHelper.ThrowIfNull(openProcessorType);

        if (!openProcessorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Type '{openProcessorType.FullName}' must be an open generic type definition (e.g., typeof(MyProcessor<,>)).",
                nameof(openProcessorType));
        }

        var implementsInterface = openProcessorType
            .GetInterfaces()
            .Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IRequestPostProcessor<,>));

        if (!implementsInterface)
        {
            throw new ArgumentException(
                $"Type '{openProcessorType.FullName}' does not implement {nameof(IRequestPostProcessor<object, object>)}<TRequest, TResponse>.",
                nameof(openProcessorType));
        }

        if (!_postProcessorTypes.Contains(openProcessorType))
        {
            _postProcessorTypes.Add(openProcessorType);
        }

        return this;
    }

    /// <summary>
    /// Enables polymorphic dispatch. When enabled, <c>Send</c> falls back to
    /// base-type handlers if no exact-type handler is registered.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    public MediatorOptions EnablePolymorphicDispatch()
    {
        PolymorphicDispatchEnabled = true;
        return this;
    }

    /// <summary>
    /// Configures the notification publish strategy. Defaults to <see cref="SequentialNotificationPublishStrategy"/>
    /// if not specified.
    /// </summary>
    /// <typeparam name="TStrategy">The strategy type implementing <see cref="INotificationPublishStrategy"/>.</typeparam>
    /// <returns>This instance for method chaining.</returns>
    public MediatorOptions UseNotificationPublishStrategy<TStrategy>()
        where TStrategy : class, INotificationPublishStrategy
    {
        NotificationPublishStrategyType = typeof(TStrategy);
        return this;
    }
}
