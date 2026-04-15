namespace Codery.Mediator;

/// <summary>
/// Marker interface for a request that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<TResponse>;

/// <summary>
/// Marker interface for a request that does not return a meaningful response.
/// </summary>
public interface IRequest : IRequest<Unit>;
