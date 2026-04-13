using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplication()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .Check(Architecture);
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void Domain_ShouldNotDependOnApi()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApiLayer)
            .Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNotDependOnApi()
    {
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(ApiLayer)
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(ApiLayer)
            .Check(Architecture);
    }
}
