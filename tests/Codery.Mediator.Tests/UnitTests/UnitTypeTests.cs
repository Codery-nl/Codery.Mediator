using FluentAssertions;

namespace Codery.Mediator.Tests.UnitTests;

public sealed class UnitTypeTests
{
    [Fact]
    public void Value_ReturnsSameDefault()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        a.Should().Be(b);
    }

    [Fact]
    public async Task Task_IsCompleted()
    {
        var task = Unit.Task;

        task.IsCompleted.Should().BeTrue();
        var result = await task;
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public void Equality_TwoUnitsAreEqual()
    {
        var a = new Unit();
        var b = default(Unit);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_ReturnsZero()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        a.CompareTo(b).Should().Be(0);
        ((IComparable)a).CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void ToString_ReturnsParentheses()
    {
        Unit.Value.ToString().Should().Be("()");
    }

    [Fact]
    public void CompareTo_NonUnitObject_ThrowsArgumentException()
    {
        var unit = Unit.Value;

        var act = () => ((IComparable)unit).CompareTo("not a unit");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompareTo_NullObject_ReturnsPositive()
    {
        var unit = Unit.Value;

        ((IComparable)unit).CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void GetHashCode_AllUnitsHaveSameHashCode()
    {
        var a = new Unit();
        var b = default(Unit);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
