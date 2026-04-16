namespace Codery.Mediator;

/// <summary>
/// Marker interface for a stream request that returns an asynchronous stream of <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of items in the response stream.</typeparam>
public interface IStreamRequest<out TResponse>;
