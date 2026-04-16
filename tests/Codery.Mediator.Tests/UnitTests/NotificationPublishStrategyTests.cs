using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Notifications;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class NotificationPublishStrategyTests
{
    [Fact]
    public async Task DefaultStrategy_IsSequential()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H2"));
        services.AddCoderyMediator(typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced("test"));

        log.Should().Equal("H1:test", "H2:test");
    }

    [Fact]
    public async Task ExplicitSequentialStrategy_InvokesAllHandlers()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H2"));
        services.AddCoderyMediator(
            opts => opts.UseNotificationPublishStrategy<SequentialNotificationPublishStrategy>(),
            typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced("seq"));

        log.Should().Equal("H1:seq", "H2:seq");
    }

    [Fact]
    public async Task ParallelStrategy_InvokesAllHandlers()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H1"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler(log, "H2"));
        services.AddCoderyMediator(
            opts => opts.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>(),
            typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced("par"));

        log.Should().Contain("H1:par").And.Contain("H2:par");
    }

    [Fact]
    public async Task ParallelStrategy_AggregatesExceptions()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandler("first"));
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandler("second"));
        services.AddCoderyMediator(
            opts => opts.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>(),
            typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("fail"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(2);
        ex.Which.InnerExceptions.Select(e => e.Message).Should().Contain("first").And.Contain("second");
    }

    [Fact]
    public async Task CustomStrategy_IsUsed()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new TrackingHandler([], "H1"));
        services.AddCoderyMediator(
            opts => opts.UseNotificationPublishStrategy<ReverseStrategy>(),
            typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var strategy = sp.GetRequiredService<INotificationPublishStrategy>();

        strategy.Should().BeOfType<ReverseStrategy>();
    }

    [Fact]
    public async Task SequentialStrategy_SingleHandlerThrows_WrapsInAggregateException()
    {
        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<OrderPlaced>>(
            _ => new ThrowingHandler("boom"));
        services.AddCoderyMediator(typeof(NotificationPublishStrategyTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Publish(new OrderPlaced("fail"));

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().ContainSingle()
            .Which.Message.Should().Be("boom");
    }

    #region Test doubles

    private sealed class TrackingHandler(List<string> log, string name) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{notification.OrderId}");
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler(string message) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);
    }

    public sealed class ReverseStrategy : INotificationPublishStrategy
    {
        public async Task PublishAsync<TNotification>(
            IEnumerable<INotificationHandler<TNotification>> handlers,
            TNotification notification,
            CancellationToken cancellationToken)
            where TNotification : INotification
        {
            foreach (var handler in handlers.Reverse())
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    #endregion
}
