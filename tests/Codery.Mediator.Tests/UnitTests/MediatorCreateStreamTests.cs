using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class MediatorCreateStreamTests
{
    private static IMediator CreateMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        configure?.Invoke(services);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task CreateStream_WithRegisteredHandler_ReturnsItems()
    {
        var mediator = CreateMediator();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("Hello", 3)))
        {
            items.Add(item);
        }

        items.Should().Equal("Pong 0: Hello", "Pong 1: Hello", "Pong 2: Hello");
    }

    [Fact]
    public async Task CreateStream_WithNoHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = async () =>
        {
            await foreach (var _ in mediator.CreateStream(new UnhandledStreamRequest()))
            {
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IStreamRequestHandler*");
    }

    [Fact]
    public void CreateStream_NullRequest_ThrowsArgumentNullException()
    {
        var mediator = CreateMediator();

        var act = () => mediator.CreateStream<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateStream_WithCancellationToken_PassesThroughToHandler()
    {
        var mediator = CreateMediator();
        using var cts = new CancellationTokenSource();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("test", 2), cts.Token))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateStream_Cancellation_StopsEnumeration()
    {
        var mediator = CreateMediator();
        using var cts = new CancellationTokenSource();

        var items = new List<string>();
        var act = async () =>
        {
            await foreach (var item in mediator.CreateStream(new StreamPing("test", 100), cts.Token))
            {
                items.Add(item);
                if (items.Count == 2)
                {
                    cts.Cancel();
                }
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateStream_ConcurrentCalls_AllSucceed()
    {
        var mediator = CreateMediator();

        var tasks = Enumerable.Range(0, 50)
            .Select(async i =>
            {
                var items = new List<string>();
                await foreach (var item in mediator.CreateStream(new StreamPing($"msg-{i}", 2)))
                {
                    items.Add(item);
                }
                return items;
            });

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(50);
        results.Should().OnlyContain(items => items.Count == 2);
    }

    [Fact]
    public async Task CreateStream_MultipleYields_PreservesOrder()
    {
        var mediator = CreateMediator();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("order", 5)))
        {
            items.Add(item);
        }

        items.Should().Equal(
            "Pong 0: order", "Pong 1: order", "Pong 2: order",
            "Pong 3: order", "Pong 4: order");
    }

    private sealed record UnhandledStreamRequest : IStreamRequest<string>;
}
