using ArchUnitNET.xUnit;
using Carter;
using Mapster;
using Microsoft.AspNetCore.Http;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchitectureTestBase;

public class ApiLayerTests
{
    // CarterModule subclasses should be in API.Endpoints
    [Fact]
    public void CarterModules_ShouldBeInEndpointsNamespace()
    {
        Classes().That()
            .AreAssignableTo(typeof(CarterModule))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.API.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.API\\.Endpoints.*")
            .Check(Architecture);
    }

    // IEndpointFilter implementations should be in API.Filters
    [Fact]
    public void EndpointFilters_ShouldBeInFiltersNamespace()
    {
        Classes().That()
            .ImplementInterface(typeof(IEndpointFilter))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.API.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.API\\.Filters.*")
            .Check(Architecture);
    }

    // Mapster IRegister implementations should be in API.Mappings
    [Fact]
    public void MappingRegistrations_ShouldBeInMappingsNamespace()
    {
        Classes().That()
            .ImplementInterface(typeof(IRegister))
            .And().ResideInNamespaceMatching("BudgetBuddy\\.API.*")
            .Should().ResideInNamespaceMatching("BudgetBuddy\\.API\\.Mappings.*")
            .Check(Architecture);
    }
}
