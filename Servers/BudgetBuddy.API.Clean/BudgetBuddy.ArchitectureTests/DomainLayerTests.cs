using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class DomainLayerTests
{
    // Domain entities should live in Domain.Entities
    [Fact]
    public void Entities_ShouldBeInEntitiesNamespace()
    {
        Classes().That()
            .ResideInNamespaceMatching("BudgetBuddy\\.Domain.*")
            .And().DoNotHaveNameEndingWith("Exception")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Enums")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Contracts")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Constants")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Exceptions")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Domain\\.Entities.*")
            .Check(Architecture);
    }

    // Types in Domain.Exceptions should inherit from Exception
    [Fact]
    public void DomainExceptions_ShouldInheritFromException()
    {
        Classes().That()
            .ResideInNamespaceMatching("BudgetBuddy\\.Domain\\.Exceptions.*")
            .Should().BeAssignableTo(typeof(Exception))
            .Check(Architecture);
    }

    // Enums should be in Domain.Enums (entity- and contract-specific enums may be co-located)
    [Fact]
    public void Enums_ShouldBeInEnumsNamespace()
    {
        Types().That()
            .AreEnums()
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Domain.*")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Entities")
            .And().DoNotResideInNamespace("BudgetBuddy.Domain.Contracts")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Domain\\.Enums.*")
            .Check(Architecture);
    }
}
