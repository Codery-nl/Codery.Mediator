namespace Codery.Mediator;

/// <summary>
/// Configuration options for the mediator pipeline.
/// </summary>
public sealed class MediatorOptions
{
    private readonly List<Type> _behaviorTypes = [];

    internal IReadOnlyList<Type> BehaviorTypes => _behaviorTypes;

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
}
