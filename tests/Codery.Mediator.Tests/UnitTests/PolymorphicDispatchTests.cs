using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class PolymorphicDispatchTests
{
    [Fact]
    public async Task Send_PolymorphicDisabled_DerivedQuery_ThrowsWhenNoExactHandler()
    {
        // Default: polymorphic dispatch is disabled
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BaseQuery, string>, BaseQueryHandler>();
        services.AddCoderyMediator(typeof(PolymorphicDispatchTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var act = () => mediator.Send(new DerivedQuery("test"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Send_PolymorphicEnabled_ExactHandlerPreferred()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BaseQuery, string>, BaseQueryHandler>();
        services.AddTransient<IRequestHandler<DerivedQuery, string>>(_ =>
            new DerivedQueryHandler());
        services.AddCoderyMediator(
            opts => opts.EnablePolymorphicDispatch(),
            typeof(PolymorphicDispatchTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var result = await mediator.Send(new DerivedQuery("test"));

        result.Should().Be("Derived: test");
    }

    [Fact]
    public async Task Send_PolymorphicEnabled_FallsBackToBaseHandler()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BaseQuery, string>, BaseQueryHandler>();
        services.AddCoderyMediator(
            opts => opts.EnablePolymorphicDispatch(),
            typeof(PolymorphicDispatchTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var result = await mediator.Send(new DerivedQuery("test"));

        result.Should().Be("Base: test");
    }

    [Fact]
    public async Task Send_PolymorphicEnabled_MultiLevelHierarchy()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BaseQuery, string>, BaseQueryHandler>();
        services.AddCoderyMediator(
            opts => opts.EnablePolymorphicDispatch(),
            typeof(PolymorphicDispatchTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var result = await mediator.Send(new DeeplyDerivedQuery("deep"));

        result.Should().Be("Base: deep");
    }

    [Fact]
    public async Task Send_PolymorphicEnabled_NoHandlerAtAnyLevel_Throws()
    {
        var services = new ServiceCollection();
        // Don't scan any assembly that has BaseQueryHandler
        services.AddCoderyMediator(
            opts => opts.EnablePolymorphicDispatch(),
            typeof(Codery.Mediator.Mediator).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var act = () => mediator.Send(new OrphanDerived("test"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private record OrphanBase(string Value) : IRequest<string>;
    private sealed record OrphanDerived(string Value) : OrphanBase(Value);

    [Fact]
    public async Task Send_PolymorphicEnabled_BaseQueryUsesOwnHandler()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BaseQuery, string>, BaseQueryHandler>();
        services.AddCoderyMediator(
            opts => opts.EnablePolymorphicDispatch(),
            typeof(PolymorphicDispatchTests).Assembly);
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Codery.Mediator.Mediator.ClearCaches();
        var result = await mediator.Send(new BaseQuery("direct"));

        result.Should().Be("Base: direct");
    }

    private sealed class DerivedQueryHandler : IRequestHandler<DerivedQuery, string>
    {
        public Task<string> Handle(DerivedQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Derived: {request.Value}");
        }
    }
}
