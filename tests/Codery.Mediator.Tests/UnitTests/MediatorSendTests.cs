using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class MediatorSendTests
{
    private static IMediator CreateMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        configure?.Invoke(services);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_WithRegisteredHandler_ReturnsResponse()
    {
        var mediator = CreateMediator();

        var result = await mediator.Send(new Ping("Hello"));

        result.Should().Be("Pong: Hello");
    }

    [Fact]
    public async Task Send_WithNoHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new UnhandledRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*IRequestHandler*");
    }

    [Fact]
    public async Task Send_VoidRequest_ReturnsUnit()
    {
        var mediator = CreateMediator();

        var result = await mediator.Send(new VoidCommand("test"));

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        var mediator = CreateMediator();

        var act = () => mediator.Send<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_WithCancellationToken_PassesTokenToHandler()
    {
        CancellationToken captured = default;
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>>(_ =>
            new CapturingPingHandler(ct => captured = ct));
        services.AddCoderyMediator(typeof(MediatorSendTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        await mediator.Send(new Ping("test"), cts.Token);

        captured.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Send_HandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>>(
            _ => new ThrowingPingHandler());
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new Ping("test"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler exploded");
    }

    [Fact]
    public async Task Send_ConcurrentCalls_AllSucceed()
    {
        var mediator = CreateMediator();

        var tasks = Enumerable.Range(0, 100)
            .Select(i => mediator.Send(new Ping($"msg-{i}")));

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(100);
        results.Should().OnlyContain(r => r.StartsWith("Pong: msg-"));
    }

    [Fact]
    public void Handlers_AreTransient_NewInstanceEachResolve()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var handler1 = sp.GetRequiredService<IRequestHandler<Ping, string>>();
        var handler2 = sp.GetRequiredService<IRequestHandler<Ping, string>>();

        handler1.Should().NotBeSameAs(handler2);
    }

    [Fact]
    public async Task Send_WithNoBehaviors_ReturnsResponse()
    {
        // Explicitly register with no behaviors to exercise the zero-behaviors path
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("no-behaviors"));

        result.Should().Be("Pong: no-behaviors");
    }

    [Fact]
    public async Task Send_CancelledToken_ThrowsOperationCanceledException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, string>>(
            _ => new CancellingPingHandler());
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => mediator.Send(new Ping("cancelled"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class CancellingPingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult("should not reach");
        }
    }

    private sealed class CapturingPingHandler(Action<CancellationToken> onHandle) : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            onHandle(cancellationToken);
            return Task.FromResult("captured");
        }
    }

    private sealed class ThrowingPingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Handler exploded");
    }
}
