using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Notifications;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class MediatorPublishTests
{
    [Fact]
    public async Task Publish_WithMultipleHandlers_CallsAllSequentially()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H2"));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced("ORD-001"));

        log.Should().Equal("H1:ORD-001", "H2:ORD-001");
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new UnhandledNotification());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_NullNotification_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish<OrderPlaced>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Publish_HandlerThrows_PropagatesAsAggregateException()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandler());
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("fail"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().ContainSingle()
            .Which.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("Handler failed");
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PassesTokenToHandlers()
    {
        CancellationToken captured = default;
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new CapturingHandler(ct => captured = ct));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        await mediator.Publish(new OrderPlaced("test"), cts.Token);

        captured.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Publish_PartialFailure_AllHandlersRun_AggregatesExceptions()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandler());
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H3"));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("partial"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().ContainSingle();

        // H1 and H3 should have run despite H2 throwing
        log.Should().Equal("H1:partial", "H3:partial");
    }

    [Fact]
    public async Task Publish_MultipleHandlersThrow_AggregatesAllExceptions()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandlerWithMessage("first"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandlerWithMessage("second"));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("multi-fail"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(2);
        ex.Which.InnerExceptions.Select(e => e.Message).Should().Equal("first", "second");
    }

    [Fact]
    public async Task Publish_WithSingleHandler_ExecutesSuccessfully()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "Solo"));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced("single"));

        log.Should().Equal("Solo:single");
    }

    [Fact]
    public async Task Publish_HandlerThrowsOperationCanceled_AggregatedAndOtherHandlersStillRun()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new CancellingNotificationHandler());
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H3"));
        services.AddSingleton<INotificationPublishStrategy, SequentialNotificationPublishStrategy>();
        services.AddTransient<IMediator, Codery.Mediator.Mediator>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("cancel-test"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().ContainSingle()
            .Which.Should().BeOfType<OperationCanceledException>();

        // H1 and H3 should have run despite H2 throwing OperationCanceledException
        log.Should().Equal("H1:cancel-test", "H3:cancel-test");
    }

    private sealed class CancellingNotificationHandler : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
            throw new OperationCanceledException("Handler cancelled");
    }

    private sealed class TrackingHandler(List<string> log, string name) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{notification.OrderId}");
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Handler failed");
    }

    private sealed class ThrowingHandlerWithMessage(string message) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);
    }

    private sealed class CapturingHandler(Action<CancellationToken> onHandle) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            onHandle(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
