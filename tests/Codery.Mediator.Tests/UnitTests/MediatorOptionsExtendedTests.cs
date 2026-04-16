using FluentAssertions;
using Codery.Mediator.Tests.Fixtures.Behaviors;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class MediatorOptionsExtendedTests
{
    #region AddOpenStreamBehavior

    [Fact]
    public void AddOpenStreamBehavior_NullType_ThrowsArgumentNullException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenStreamBehavior(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOpenStreamBehavior_NonGenericType_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenStreamBehavior(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*open generic*");
    }

    [Fact]
    public void AddOpenStreamBehavior_ValidType_Succeeds()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenStreamBehavior(typeof(LoggingStreamBehavior<,>));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenStreamBehavior_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.AddOpenStreamBehavior(typeof(LoggingStreamBehavior<,>));

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddOpenStreamBehavior_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenStreamBehavior(typeof(List<>));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*IStreamPipelineBehavior*");
    }

    [Fact]
    public void AddOpenStreamBehavior_SameTypeTwice_RegistersOnlyOnce()
    {
        var options = new MediatorOptions();

        options.AddOpenStreamBehavior(typeof(LoggingStreamBehavior<,>));
        options.AddOpenStreamBehavior(typeof(LoggingStreamBehavior<,>));

        options.StreamBehaviorTypes.Should().ContainSingle();
    }

    #endregion

    #region AddOpenPreProcessor

    [Fact]
    public void AddOpenPreProcessor_NullType_ThrowsArgumentNullException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPreProcessor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOpenPreProcessor_NonGenericType_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPreProcessor(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*open generic*");
    }

    [Fact]
    public void AddOpenPreProcessor_ValidType_Succeeds()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPreProcessor(typeof(GenericPreProcessor<>));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenPreProcessor_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.AddOpenPreProcessor(typeof(GenericPreProcessor<>));

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddOpenPreProcessor_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPreProcessor(typeof(List<>));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*IRequestPreProcessor*");
    }

    [Fact]
    public void AddOpenPreProcessor_SameTypeTwice_RegistersOnlyOnce()
    {
        var options = new MediatorOptions();

        options.AddOpenPreProcessor(typeof(GenericPreProcessor<>));
        options.AddOpenPreProcessor(typeof(GenericPreProcessor<>));

        options.PreProcessorTypes.Should().ContainSingle();
    }

    #endregion

    #region AddOpenPostProcessor

    [Fact]
    public void AddOpenPostProcessor_NullType_ThrowsArgumentNullException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPostProcessor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOpenPostProcessor_NonGenericType_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPostProcessor(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*open generic*");
    }

    [Fact]
    public void AddOpenPostProcessor_ValidType_Succeeds()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPostProcessor(typeof(GenericPostProcessor<,>));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenPostProcessor_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.AddOpenPostProcessor(typeof(GenericPostProcessor<,>));

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddOpenPostProcessor_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenPostProcessor(typeof(List<>));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*IRequestPostProcessor*");
    }

    [Fact]
    public void AddOpenPostProcessor_SameTypeTwice_RegistersOnlyOnce()
    {
        var options = new MediatorOptions();

        options.AddOpenPostProcessor(typeof(GenericPostProcessor<,>));
        options.AddOpenPostProcessor(typeof(GenericPostProcessor<,>));

        options.PostProcessorTypes.Should().ContainSingle();
    }

    #endregion

    #region EnablePolymorphicDispatch

    [Fact]
    public void EnablePolymorphicDispatch_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.EnablePolymorphicDispatch();

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void EnablePolymorphicDispatch_SetsFlag()
    {
        var options = new MediatorOptions();

        options.EnablePolymorphicDispatch();

        options.PolymorphicDispatchEnabled.Should().BeTrue();
    }

    #endregion

    #region UseNotificationPublishStrategy

    [Fact]
    public void UseNotificationPublishStrategy_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>();

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void UseNotificationPublishStrategy_SetsType()
    {
        var options = new MediatorOptions();

        options.UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>();

        options.NotificationPublishStrategyType.Should().Be(typeof(ParallelNotificationPublishStrategy));
    }

    #endregion

    #region Chaining

    [Fact]
    public void AllMethods_CanBeChained()
    {
        var options = new MediatorOptions();

        var act = () => options
            .AddOpenBehavior(typeof(LoggingBehavior<,>))
            .AddOpenStreamBehavior(typeof(LoggingStreamBehavior<,>))
            .AddOpenPreProcessor(typeof(GenericPreProcessor<>))
            .AddOpenPostProcessor(typeof(GenericPostProcessor<,>))
            .EnablePolymorphicDispatch()
            .UseNotificationPublishStrategy<ParallelNotificationPublishStrategy>();

        act.Should().NotThrow();
    }

    #endregion

    #region Test types

    public sealed class GenericPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class GenericPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}
