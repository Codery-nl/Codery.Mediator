using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class RequestPrePostProcessorTests
{
    [Fact]
    public async Task PreProcessor_RunsBeforeHandler()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IRequestPreProcessor<Ping>>(_ => new TrackingPreProcessor(log, "Pre"));
        services.AddTransient<IRequestHandler<Ping, string>>(_ => new TrackingPingHandler(log));
        services.AddCoderyMediator(typeof(RequestPrePostProcessorTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        log.Should().Equal("Pre:test", "Handler:test");
    }

    [Fact]
    public async Task PostProcessor_RunsAfterHandler()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IRequestPostProcessor<Ping, string>>(_ => new TrackingPostProcessor(log, "Post"));
        services.AddTransient<IRequestHandler<Ping, string>>(_ => new TrackingPingHandler(log));
        services.AddCoderyMediator(typeof(RequestPrePostProcessorTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        log.Should().Equal("Handler:test", "Post:test=Pong: test");
    }

    [Fact]
    public async Task PreAndPost_RunInCorrectOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IRequestPreProcessor<Ping>>(_ => new TrackingPreProcessor(log, "Pre"));
        services.AddTransient<IRequestPostProcessor<Ping, string>>(_ => new TrackingPostProcessor(log, "Post"));
        services.AddTransient<IRequestHandler<Ping, string>>(_ => new TrackingPingHandler(log));
        services.AddCoderyMediator(typeof(RequestPrePostProcessorTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        // Pre runs outermost (first), then handler, then post (innermost, right around handler)
        log.Should().Equal("Pre:test", "Handler:test", "Post:test=Pong: test");
    }

    [Fact]
    public async Task MultiplePreProcessors_RunInOrder()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddTransient<IRequestPreProcessor<Ping>>(_ => new TrackingPreProcessor(log, "Pre1"));
        services.AddTransient<IRequestPreProcessor<Ping>>(_ => new TrackingPreProcessor(log, "Pre2"));
        services.AddTransient<IRequestHandler<Ping, string>>(_ => new TrackingPingHandler(log));
        services.AddCoderyMediator(typeof(RequestPrePostProcessorTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("test"));

        log.Should().Equal("Pre1:test", "Pre2:test", "Handler:test");
    }

    [Fact]
    public async Task NoProcessorsRegistered_PipelineIsNoOp()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("test"));

        result.Should().Be("Pong: test");
    }

    [Fact]
    public async Task PreProcessor_Throws_StopsPipeline()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestPreProcessor<Ping>>(_ => new ThrowingPreProcessor());
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new Ping("test"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pre-processor failed");
    }

    [Fact]
    public async Task PostProcessor_ReceivesResponse()
    {
        string? capturedResponse = null;
        var services = new ServiceCollection();
        services.AddTransient<IRequestPostProcessor<Ping, string>>(
            _ => new CapturingPostProcessor(r => capturedResponse = r));
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("capture"));

        capturedResponse.Should().Be("Pong: capture");
    }

    #region Test doubles

    private sealed class TrackingPreProcessor(List<string> log, string name) : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{request.Message}");
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingPostProcessor(List<string> log, string name) : IRequestPostProcessor<Ping, string>
    {
        public Task Process(Ping request, string response, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{request.Message}={response}");
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingPingHandler(List<string> log) : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            log.Add($"Handler:{request.Message}");
            return Task.FromResult($"Pong: {request.Message}");
        }
    }

    private sealed class ThrowingPreProcessor : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Pre-processor failed");
    }

    private sealed class CapturingPostProcessor(Action<string> onProcess) : IRequestPostProcessor<Ping, string>
    {
        public Task Process(Ping request, string response, CancellationToken cancellationToken)
        {
            onProcess(response);
            return Task.CompletedTask;
        }
    }

    #endregion
}
