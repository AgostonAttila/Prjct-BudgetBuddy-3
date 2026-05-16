using BudgetBuddy.Shared.Kernel.Exceptions;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.SharedKernel.UnitTests.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_ShouldInclude_EntityNameInMessage()
    {
        var ex = new NotFoundException("Account", Guid.NewGuid());

        ex.Message.Should().Contain("Account");
    }

    [Fact]
    public void Constructor_ShouldInclude_KeyInMessage()
    {
        var key = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000");
        var ex = new NotFoundException("Transaction", key);

        ex.Message.Should().Contain(key.ToString());
    }

    [Fact]
    public void Constructor_WithStringKey_ShouldWork()
    {
        var ex = new NotFoundException("Budget", "budget-123");

        ex.Message.Should().Contain("Budget");
        ex.Message.Should().Contain("budget-123");
    }

    [Fact]
    public void NotFoundException_ShouldBe_DomainException()
    {
        var ex = new NotFoundException("X", 1);

        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void NotFoundException_ShouldBe_Exception()
    {
        var ex = new NotFoundException("X", 1);

        ex.Should().BeAssignableTo<Exception>();
    }
}
