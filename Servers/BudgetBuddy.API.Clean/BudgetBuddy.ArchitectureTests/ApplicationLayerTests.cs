using ArchUnitNET.xUnit;
using FluentValidation;
using MediatR;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class ApplicationLayerTests
{
    // IRequestHandler<,> implementations should live in Application.Features.*
    [Fact]
    public void Handlers_ShouldBeInFeaturesNamespace()
    {
        Classes().That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Application\\.Features.*")
            .Check(Architecture);
    }

    // Application interfaces should be in Application.Common.Contracts
    [Fact]
    public void ServiceInterfaces_ShouldBeInCommonContracts()
    {
        Interfaces().That()
            .ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .And().DoNotResideInNamespaceMatching("BudgetBuddy\\.Application\\.Features.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Application\\.Common.*")
            .Check(Architecture);
    }

    // IPipelineBehavior<,> implementations should be in Application.Common.Behaviors
    [Fact]
    public void PipelineBehaviors_ShouldBeInCommonBehaviors()
    {
        Classes().That()
            .ImplementInterface(typeof(IPipelineBehavior<,>))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Application\\.Common\\.Behaviors.*")
            .Check(Architecture);
    }

    // AbstractValidator<> subclasses in Application should be in Application.Features.*
    [Fact]
    public void Validators_ShouldBeInFeaturesNamespace()
    {
        Classes().That()
            .AreAssignableTo(typeof(AbstractValidator<>))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Application.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Application\\.Features.*")
            .Check(Architecture);
    }
}
