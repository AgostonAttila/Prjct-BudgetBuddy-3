using ArchUnitNET.xUnit;
using Carter;
using FluentValidation;
using MediatR;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class NamingConventionTests
{
    // Types named "*Command" in Application should implement IRequest<T>
    [Fact]
    public void Commands_ShouldEndWithCommand()
    {
        Classes().That()
            .HaveNameEndingWith("Command")
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().ImplementInterface(typeof(IRequest<>))
            .Check(Architecture);
    }

    // Types named "*Query" in Application should implement IRequest<T>
    [Fact]
    public void Queries_ShouldEndWithQuery()
    {
        Classes().That()
            .HaveNameEndingWith("Query")
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().ImplementInterface(typeof(IRequest<>))
            .Check(Architecture);
    }

    // Types implementing IRequestHandler<,> should end with "Handler"
    [Fact]
    public void Handlers_ShouldEndWithHandler()
    {
        Classes().That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().HaveNameEndingWith("Handler")
            .Check(Architecture);
    }

    // Types inheriting AbstractValidator<> should end with "Validator"
    [Fact]
    public void Validators_ShouldEndWithValidator()
    {
        Classes().That()
            .AreAssignableTo(typeof(AbstractValidator<>))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().HaveNameEndingWith("Validator")
            .Check(Architecture);
    }

    // Types inheriting CarterModule should end with "Module"
    [Fact]
    public void CarterModules_ShouldEndWithModule()
    {
        Classes().That()
            .AreAssignableTo(typeof(CarterModule))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.API.*")
            .Should().HaveNameEndingWith("Module")
            .Check(Architecture);
    }

    // All interfaces in BudgetBuddy namespace should start with "I"
    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        Interfaces().That()
            .ResideInNamespaceMatching("BudgetBuddy.*")
            .Should().HaveNameStartingWith("I")
            .Check(Architecture);
    }
}
