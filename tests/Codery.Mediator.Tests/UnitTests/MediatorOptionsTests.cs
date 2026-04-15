using FluentAssertions;
using Codery.Mediator.Tests.Fixtures.Behaviors;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class MediatorOptionsTests
{
    [Fact]
    public void AddOpenBehavior_NullType_ThrowsArgumentNullException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenBehavior(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOpenBehavior_NonGenericType_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenBehavior(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*open generic*");
    }

    [Fact]
    public void AddOpenBehavior_ValidType_Succeeds()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenBehavior(typeof(LoggingBehavior<,>));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddOpenBehavior_ReturnsSelf_ForChaining()
    {
        var options = new MediatorOptions();

        var result = options.AddOpenBehavior(typeof(LoggingBehavior<,>));

        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddOpenBehavior_TypeNotImplementingInterface_ThrowsArgumentException()
    {
        var options = new MediatorOptions();

        var act = () => options.AddOpenBehavior(typeof(List<>));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*IPipelineBehavior*");
    }

    [Fact]
    public void AddOpenBehavior_SameTypeTwice_RegistersOnlyOnce()
    {
        var options = new MediatorOptions();

        options.AddOpenBehavior(typeof(LoggingBehavior<,>));
        options.AddOpenBehavior(typeof(LoggingBehavior<,>));

        options.BehaviorTypes.Should().ContainSingle();
    }
}
