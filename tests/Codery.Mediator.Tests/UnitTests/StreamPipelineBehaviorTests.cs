using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class StreamPipelineBehaviorTests
{
    [Fact]
    public async Task CreateStream_WithBehavior_WrapsHandler()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IStreamPipelineBehavior<StreamPing, string>>(
            _ => new TrackingStreamBehavior<StreamPing, string>(log, "Outer"));
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("test", 2)))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        log.Should().Equal("Outer:Before", "Outer:After");
    }

    [Fact]
    public async Task CreateStream_MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IStreamPipelineBehavior<StreamPing, string>>(
            _ => new TrackingStreamBehavior<StreamPing, string>(log, "First"));
        services.AddTransient<IStreamPipelineBehavior<StreamPing, string>>(
            _ => new TrackingStreamBehavior<StreamPing, string>(log, "Second"));
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await foreach (var _ in mediator.CreateStream(new StreamPing("test", 1)))
        {
        }

        log.Should().Equal("First:Before", "Second:Before", "Second:After", "First:After");
    }

    [Fact]
    public async Task CreateStream_BehaviorShortCircuits_HandlerNotCalled()
    {
        var services = new ServiceCollection();
        services.AddTransient<IStreamPipelineBehavior<StreamPing, string>>(
            _ => new ShortCircuitStreamBehavior());
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("test", 10)))
        {
            items.Add(item);
        }

        items.Should().Equal("short-circuited");
    }

    [Fact]
    public async Task CreateStream_BehaviorCanFilterStream()
    {
        var services = new ServiceCollection();
        services.AddTransient<IStreamPipelineBehavior<StreamPing, string>>(
            _ => new FilteringStreamBehavior());
        services.AddCoderyMediator(typeof(StreamPingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var items = new List<string>();
        await foreach (var item in mediator.CreateStream(new StreamPing("test", 4)))
        {
            items.Add(item);
        }

        // FilteringStreamBehavior only yields items containing "0" or "2"
        items.Should().Equal("Pong 0: test", "Pong 2: test");
    }

    #region Test doubles

    private sealed class TrackingStreamBehavior<TRequest, TResponse>(List<string> log, string name)
        : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        public async IAsyncEnumerable<TResponse> Handle(
            TRequest request,
            StreamHandlerDelegate<TResponse> next,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            log.Add($"{name}:Before");
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            log.Add($"{name}:After");
        }
    }

    private sealed class ShortCircuitStreamBehavior : IStreamPipelineBehavior<StreamPing, string>
    {
        public async IAsyncEnumerable<string> Handle(
            StreamPing request,
            StreamHandlerDelegate<string> next,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return "short-circuited";
        }
    }

    private sealed class FilteringStreamBehavior : IStreamPipelineBehavior<StreamPing, string>
    {
        public async IAsyncEnumerable<string> Handle(
            StreamPing request,
            StreamHandlerDelegate<string> next,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                if (item.Contains('0') || item.Contains('2'))
                {
                    yield return item;
                }
            }
        }
    }

    #endregion
}
