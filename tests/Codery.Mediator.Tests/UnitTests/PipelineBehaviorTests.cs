using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Behaviors;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task Send_WithNoBehaviors_SkipsPipelineConstruction()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("no-pipeline"));

        result.Should().Be("Pong: no-pipeline");
    }

    [Fact]
    public async Task Send_WithOneBehavior_BehaviorWrapsHandler()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Outer"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("test"));

        result.Should().Be("Pong: test");
        log.Should().Equal("Outer:Before", "Outer:After");
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "First"));
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Second"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        log.Should().Equal("First:Before", "Second:Before", "Second:After", "First:After");
    }

    [Fact]
    public async Task Send_WithThreeBehaviors_ExecuteInCorrectOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Auth"));
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Validation"));
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Logging"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        log.Should().Equal(
            "Auth:Before", "Validation:Before", "Logging:Before",
            "Logging:After", "Validation:After", "Auth:After");
    }

    [Fact]
    public async Task Send_BehaviorShortCircuits_HandlerNotCalled()
    {
        var handlerCalled = false;
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new ShortCircuitBehavior<Ping, string>("short-circuited"));
        services.AddTransient<IRequestHandler<Ping, string>>(
            _ => new TrackingPingHandler(() => handlerCalled = true));
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("test"));

        result.Should().Be("short-circuited");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Send_BehaviorThrowsBeforeNext_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new ThrowingBehavior<Ping, string>(throwBefore: true));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new Ping("test"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Behavior failed before next");
    }

    [Fact]
    public async Task Send_BehaviorThrowsAfterNext_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new ThrowingBehavior<Ping, string>(throwBefore: false));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new Ping("test"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Behavior failed after next");
    }

    [Fact]
    public async Task Send_CancellationTokenPassedThroughBehavior()
    {
        CancellationToken capturedInBehavior = default;
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new CapturingBehavior<Ping, string>(ct => capturedInBehavior = ct));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        await mediator.Send(new Ping("test"), cts.Token);

        capturedInBehavior.Should().Be(cts.Token);
    }

    #region Test doubles

    private sealed class TrackingBehavior<TRequest, TResponse>(List<string> log, string name)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add($"{name}:Before");
            var response = await next();
            log.Add($"{name}:After");
            return response;
        }
    }

    private sealed class ThrowingBehavior<TRequest, TResponse>(bool throwBefore)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (throwBefore)
                throw new InvalidOperationException("Behavior failed before next");

            var response = await next();
            throw new InvalidOperationException("Behavior failed after next");
        }
    }

    private sealed class CapturingBehavior<TRequest, TResponse>(Action<CancellationToken> onHandle)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            onHandle(cancellationToken);
            return next();
        }
    }

    private sealed class TrackingPingHandler(Action onHandle) : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            onHandle();
            return Task.FromResult($"Pong: {request.Message}");
        }
    }

    #endregion
}
