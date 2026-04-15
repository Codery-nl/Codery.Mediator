namespace Codery.Mediator;

/// <summary>
/// Represents a void type since <see cref="System.Void"/> is not a valid generic type argument.
/// </summary>
public readonly record struct Unit : IComparable<Unit>, IComparable
{
    /// <summary>Gets the singleton <see cref="Unit"/> value.</summary>
    public static readonly Unit Value = default;

    /// <summary>Gets a completed <see cref="Task{Unit}"/> with the default <see cref="Unit"/> value.</summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(default(Unit));

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj switch
    {
        Unit => 0,
        null => 1,
        _ => throw new ArgumentException($"Object must be of type {nameof(Unit)}.", nameof(obj))
    };

    /// <inheritdoc />
    public override string ToString() => "()";
}
