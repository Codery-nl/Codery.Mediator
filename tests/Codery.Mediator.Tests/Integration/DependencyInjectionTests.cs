using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Notifications;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.Integration;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task FullPipeline_RequestWithBehavior_WorksEndToEnd()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IPipelineBehavior<Ping, string>>(
            _ => new TrackingBehavior<Ping, string>(log, "Log"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new Ping("integration"));

        result.Should().Be("Pong: integration");
        log.Should().ContainInOrder("Log:Before", "Log:After");
    }

    [Fact]
    public async Task FullPipeline_NotificationWithMultipleHandlers_AllCalled()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingNotificationHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingNotificationHandler(log, "H2"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Publish(new OrderPlaced("INT-001"));

        log.Should().Equal("H1:INT-001", "H2:INT-001");
    }

    [Fact]
    public async Task FullPipeline_VoidCommand_CompletesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(VoidCommandHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new VoidCommand("integration"));

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task ISender_CanBeInjectedDirectly()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var result = await sender.Send(new Ping("via-sender"));

        result.Should().Be("Pong: via-sender");
    }

    [Fact]
    public async Task IPublisher_CanBeInjectedDirectly()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingNotificationHandler(log, "Direct"));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var publisher = sp.GetRequiredService<IPublisher>();
        await publisher.Publish(new OrderPlaced("via-publisher"));

        log.Should().Equal("Direct:via-publisher");
    }

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

    private sealed class TrackingNotificationHandler(List<string> log, string name)
        : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{notification.OrderId}");
            return Task.CompletedTask;
        }
    }
}
