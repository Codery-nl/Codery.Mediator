using System.Runtime.CompilerServices;
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
    private IMediator _mediatorParallel = null!;
    private IMediator _mediatorWithPrePost = null!;
    private IServiceProvider _serviceProvider = null!;
    private PingRequest _request = null!;
    private PongNotification _notification = null!;
    private StreamPingRequest _streamRequest = null!;

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

        // Mediator with parallel publish strategy
        var servicesParallel = new ServiceCollection();
        servicesParallel.AddCoderyMediator(
            opts => opts.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>(),
            typeof(MediatorBenchmarks).Assembly);
        var spParallel = servicesParallel.BuildServiceProvider();
        _mediatorParallel = spParallel.GetRequiredService<IMediator>();

        // Mediator with pre/post processors
        var servicesPrePost = new ServiceCollection();
        servicesPrePost.AddTransient<IRequestPreProcessor<PingRequest>, NoOpPreProcessor>();
        servicesPrePost.AddTransient<IRequestPostProcessor<PingRequest, string>, NoOpPostProcessor>();
        servicesPrePost.AddCoderyMediator(typeof(MediatorBenchmarks).Assembly);
        var spPrePost = servicesPrePost.BuildServiceProvider();
        _mediatorWithPrePost = spPrePost.GetRequiredService<IMediator>();

        _request = new PingRequest("benchmark");
        _notification = new PongNotification("benchmark");
        _streamRequest = new StreamPingRequest("benchmark");

        // Warm up caches
        _mediator.Send(_request).GetAwaiter().GetResult();
        _mediator.Publish(_notification).GetAwaiter().GetResult();
        _mediatorWithBehaviors.Send(_request).GetAwaiter().GetResult();
        _mediatorParallel.Publish(_notification).GetAwaiter().GetResult();
        _mediatorWithPrePost.Send(_request).GetAwaiter().GetResult();
        ConsumeStream(_mediator.CreateStream(_streamRequest)).GetAwaiter().GetResult();
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
    public Task<string> MediatorSendWithPrePostProcessors()
    {
        return _mediatorWithPrePost.Send(_request);
    }

    [Benchmark]
    public Task NotificationPublish()
    {
        return _mediator.Publish(_notification);
    }

    [Benchmark]
    public Task NotificationPublishParallel()
    {
        return _mediatorParallel.Publish(_notification);
    }

    [Benchmark]
    public Task StreamRequest()
    {
        return ConsumeStream(_mediator.CreateStream(_streamRequest));
    }

    private static async Task ConsumeStream(IAsyncEnumerable<string> stream)
    {
        await foreach (var _ in stream)
        {
        }
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

public sealed record StreamPingRequest(string Message) : IStreamRequest<string>;

public sealed class StreamPingRequestHandler : IStreamRequestHandler<StreamPingRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        StreamPingRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return $"Pong 0: {request.Message}";
        yield return $"Pong 1: {request.Message}";
        yield return $"Pong 2: {request.Message}";
    }
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

public sealed class NoOpPreProcessor : IRequestPreProcessor<PingRequest>
{
    public Task Process(PingRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class NoOpPostProcessor : IRequestPostProcessor<PingRequest, string>
{
    public Task Process(PingRequest request, string response, CancellationToken cancellationToken) => Task.CompletedTask;
}

#endregion
