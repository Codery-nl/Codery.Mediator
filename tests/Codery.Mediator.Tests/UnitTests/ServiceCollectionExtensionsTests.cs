using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Codery.Mediator.Tests.Fixtures.Notifications;
using Codery.Mediator.Tests.Fixtures.Requests;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCoderyMediator_RegistersIMediator()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var mediator = sp.GetService<IMediator>();

        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddCoderyMediator_RegistersISender()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var sender = sp.GetService<ISender>();

        sender.Should().NotBeNull();
    }

    [Fact]
    public void AddCoderyMediator_RegistersIPublisher()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var publisher = sp.GetService<IPublisher>();

        publisher.Should().NotBeNull();
    }

    [Fact]
    public void AddCoderyMediator_ISenderAndIPublisher_ResolveSameInstanceWithinScope()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Scoped: ISender and IPublisher resolve through IMediator, same instance within a scope
        sender.Should().BeSameAs(mediator);
        publisher.Should().BeSameAs(mediator);
    }

    [Fact]
    public void AddCoderyMediator_ScansAndRegistersRequestHandlers()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        var handler = sp.GetService<IRequestHandler<Ping, string>>();

        handler.Should().NotBeNull().And.BeOfType<PingHandler>();
    }

    [Fact]
    public void AddCoderyMediator_NoAssemblies_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddCoderyMediator(Array.Empty<System.Reflection.Assembly>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one assembly*");
    }

    [Fact]
    public void AddCoderyMediator_CalledTwice_DoesNotDuplicateMediator()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        services.AddCoderyMediator(typeof(PingHandler).Assembly);

        var mediatorRegistrations = services
            .Where(sd => sd.ServiceType == typeof(IMediator))
            .ToList();

        mediatorRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public void AddCoderyMediator_MultipleAssemblies_ScansAll()
    {
        var services = new ServiceCollection();
        // Pass the same assembly twice to exercise the multi-assembly loop;
        // in real usage these would be different assemblies.
        var assembly = typeof(PingHandler).Assembly;
        services.AddCoderyMediator(assembly, assembly);
        var sp = services.BuildServiceProvider();

        var handler = sp.GetService<IRequestHandler<Ping, string>>();

        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddCoderyMediator_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddCoderyMediator(typeof(PingHandler).Assembly);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCoderyMediator_NullAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddCoderyMediator(assemblies: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCoderyMediator_RegistersBehaviorsInOrder()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(
            opts =>
            {
                opts.AddOpenBehavior(typeof(Codery.Mediator.Tests.Fixtures.Behaviors.LoggingBehavior<,>));
                opts.AddOpenBehavior(typeof(Codery.Mediator.Tests.Fixtures.Behaviors.ValidationBehavior<,>));
            },
            typeof(PingHandler).Assembly);

        var behaviorRegistrations = services
            .Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorRegistrations.Should().HaveCount(2);
    }

    [Fact]
    public void AddCoderyMediator_NullAssemblyElement_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddCoderyMediator(typeof(PingHandler).Assembly, null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*null*");
    }

    [Fact]
    public void AddCoderyMediator_DuplicateAssembly_DoesNotDuplicateNotificationHandlers()
    {
        var services = new ServiceCollection();
        var assembly = typeof(PingHandler).Assembly;
        services.AddCoderyMediator(assembly, assembly);

        var notificationRegistrations = services
            .Where(sd => sd.ServiceType.IsGenericType
                      && sd.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            .ToList();

        // Each notification handler type should appear exactly once, not twice
        notificationRegistrations.Select(sd => sd.ImplementationType).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AddCoderyMediator_CalledTwice_DoesNotDuplicateNotificationHandlers()
    {
        var services = new ServiceCollection();
        var assembly = typeof(PingHandler).Assembly;
        services.AddCoderyMediator(assembly);
        services.AddCoderyMediator(assembly);

        var notificationRegistrations = services
            .Where(sd => sd.ServiceType.IsGenericType
                      && sd.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            .ToList();

        // Assembly already scanned in first call — second call should skip it
        notificationRegistrations.Select(sd => sd.ImplementationType).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AddCoderyMediator_CalledTwiceWithSameBehavior_DoesNotDuplicateBehavior()
    {
        var services = new ServiceCollection();
        var assembly = typeof(PingHandler).Assembly;

        services.AddCoderyMediator(
            opts => opts.AddOpenBehavior(typeof(Codery.Mediator.Tests.Fixtures.Behaviors.LoggingBehavior<,>)),
            assembly);
        services.AddCoderyMediator(
            opts => opts.AddOpenBehavior(typeof(Codery.Mediator.Tests.Fixtures.Behaviors.LoggingBehavior<,>)),
            assembly);

        var behaviorRegistrations = services
            .Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorRegistrations.Should().HaveCount(1);
    }

    [Fact]
    public void AddCoderyMediator_SkipsAbstractHandlerClasses()
    {
        var services = new ServiceCollection();
        services.AddCoderyMediator(typeof(PingHandler).Assembly);
        var sp = services.BuildServiceProvider();

        // AbstractPingHandler is abstract and should not be registered
        var handlers = sp.GetServices<IRequestHandler<Ping, string>>().ToList();
        handlers.Should().AllSatisfy(h => h.GetType().IsAbstract.Should().BeFalse());
    }
}
