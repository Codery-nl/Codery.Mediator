using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator;

namespace Codery.Mediator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MediatorBenchmarks
{
    private IMediator _mediator = null!;
    private IMediator _mediatorWithBehaviors = null!;
    private IServiceProvider _serviceProvider = null!;
    private PingRequest _request = null!;
    private PongNotification _notification = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Mediator without behaviors
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(MediatorBenchmarks).Assembly);
        var sp = services.BuildServiceProvider();
        _serviceProvider = sp;
        _mediator = sp.GetRequiredService<IMediator>();

        // Mediator with 3 behaviors
        var servicesWithBehaviors = new ServiceCollection();
        servicesWithBehaviors.AddCoderyMediator(
            opts =>
            {
                opts.AddOpenBehavior(typeof(NoOpBehavior1<,>));
                opts.AddOpenBehavior(typeof(NoOpBehavior2<,>));
                opts.AddOpenBehavior(typeof(NoOpBehavior3<,>));
            },
            typeof(MediatorBenchmarks).Assembly);
        var spWithBehaviors = servicesWithBehaviors.BuildServiceProvider();
        _mediatorWithBehaviors = spWithBehaviors.GetRequiredService<IMediator>();

        _request = new PingRequest("benchmark");
        _notification = new PongNotification("benchmark");

        // Warm up caches
        _mediator.Send(_request).GetAwaiter().GetResult();
        _mediator.Publish(_notification).GetAwaiter().GetResult();
        _mediatorWithBehaviors.Send(_request).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task<string> DirectHandlerCall()
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<PingRequest, string>>();
        return handler.Handle(_request, CancellationToken.None);
    }

    [Benchmark]
    public Task<string> MediatorSend()
    {
        return _mediator.Send(_request);
    }

    [Benchmark]
    public Task<string> MediatorSendWithThreeBehaviors()
    {
        return _mediatorWithBehaviors.Send(_request);
    }

    [Benchmark]
    public Task NotificationPublish()
    {
        return _mediator.Publish(_notification);
    }
}

#region Benchmark types

public sealed record PingRequest(string Message) : IRequest<string>;

public sealed class PingRequestHandler : IRequestHandler<PingRequest, string>
{
    public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Pong: {request.Message}");
    }
}

public sealed record PongNotification(string Message) : INotification;

public sealed class PongHandler1 : INotificationHandler<PongNotification>
{
    public Task Handle(PongNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PongHandler2 : INotificationHandler<PongNotification>
{
    public Task Handle(PongNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class NoOpBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
}

public sealed class NoOpBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
}

public sealed class NoOpBehavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
}

#endregion
