using FluentAssertions;
using NetArchTest.Rules;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Shared réteg tisztaság: Shared.Kernel, Shared.Contracts és Shared.Infrastructure
/// nem hivatkozhatnak modul-specifikus belső típusokra (mmrule.md – 3. Shared/Kernel).
/// </summary>
public class SharedLayerPurityTests
{
    [Fact]
    public void SharedKernel_ShouldNotDependOnAnyModule()
    {
        var result = Types.InAssembly(Assemblies.SharedKernel)
            .Should()
            .NotHaveDependencyOnAny(Assemblies.AllModuleNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Kernel modul-specifikus típusokat nem tartalmazhat. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void SharedContracts_ShouldNotDependOnAnyModule()
    {
        var result = Types.InAssembly(Assemblies.SharedContracts)
            .Should()
            .NotHaveDependencyOnAny(Assemblies.AllModuleNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Contracts nem hivatkozhat modul belső típusaira (csak interface-ek és DTO-k lehetnek benne). Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void SharedInfrastructure_ShouldNotDependOnAnyModule()
    {
        var result = Types.InAssembly(Assemblies.SharedInfrastructure)
            .Should()
            .NotHaveDependencyOnAny(Assemblies.AllModuleNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Shared.Infrastructure nem hivatkozhat modul belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result) =>
        result.FailingTypeNames is null
            ? "(nincs adat)"
            : string.Join(", ", result.FailingTypeNames);
}
