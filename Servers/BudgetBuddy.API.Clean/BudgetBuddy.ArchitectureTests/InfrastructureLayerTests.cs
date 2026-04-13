using ArchUnitNET.xUnit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class InfrastructureLayerTests
{
    // DbContext subclasses should be in Infrastructure.Persistence
    [Fact]
    public void DbContext_ShouldBeInPersistenceNamespace()
    {
        Classes().That()
            .AreAssignableTo(typeof(DbContext))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Infrastructure.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Infrastructure\\.Persistence.*")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    // IEntityTypeConfiguration<> implementations should be in Infrastructure.Persistence.Configurations
    [Fact]
    public void EntityConfigurations_ShouldBeInPersistenceConfigurations()
    {
        Classes().That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.Infrastructure.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.Infrastructure\\.Persistence\\.Configurations.*")
            .Check(Architecture);
    }
}
