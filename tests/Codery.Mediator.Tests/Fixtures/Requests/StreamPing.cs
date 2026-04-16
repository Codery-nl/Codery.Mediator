using System.Runtime.CompilerServices;

namespace Codery.Mediator.Tests.Fixtures.Requests;

public sealed record StreamPing(string Message, int Count = 3) : IStreamRequest<string>;

public sealed class StreamPingHandler : IStreamRequestHandler<StreamPing, string>
{
    public async IAsyncEnumerable<string> Handle(
        StreamPing request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return $"Pong {i}: {request.Message}";
        }
    }
}
