namespace Codery.Mediator.Internal;

/// <summary>
/// Internal configuration for the mediator, registered as a singleton.
/// </summary>
internal sealed class MediatorConfiguration
{
    /// <summary>
    /// Gets a value indicating whether polymorphic dispatch is enabled.
    /// </summary>
    public bool PolymorphicDispatchEnabled { get; init; }
}
