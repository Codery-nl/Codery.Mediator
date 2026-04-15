namespace Codery.Mediator;

/// <summary>
/// Defines a mediator to encapsulate request/response and notification patterns.
/// Combines <see cref="ISender"/> and <see cref="IPublisher"/>.
/// </summary>
public interface IMediator : ISender, IPublisher;
