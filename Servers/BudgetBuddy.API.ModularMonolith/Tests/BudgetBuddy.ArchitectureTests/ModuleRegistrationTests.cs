using System.Reflection;
using FluentAssertions;
using BudgetBuddy.Shared.Contracts;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Minden modul assembly-ben pontosan egy IModule implementáció létezik
/// (mmrule.md – 1. Modul határok: IModule interfész).
/// </summary>
public class ModuleRegistrationTests
{
    public static IEnumerable<object[]> ModuleAssemblies() =>
        Assemblies.AllModules.Select(a => new object[] { a.GetName().Name!, a });

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void EachModule_ShouldHaveExactlyOneIModuleImplementation(string assemblyName, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IModule).IsAssignableFrom(t))
            .ToList();

        implementations.Should().HaveCount(1,
            because: $"{assemblyName} assembly-nek pontosan 1 IModule implementációt kell tartalmaznia, " +
                     $"de {implementations.Count} található: {string.Join(", ", implementations.Select(t => t.Name))}");
    }

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void EachModule_IModuleImplementation_ShouldBeNamedAfterModule(string assemblyName, Assembly assembly)
    {
        var implementation = assembly.GetTypes()
            .Single(t => t is { IsClass: true, IsAbstract: false }
                         && typeof(IModule).IsAssignableFrom(t));

        // pl. BudgetBuddy.Module.Accounts → AccountsModule
        implementation.Name.Should().EndWith("Module",
            because: $"{assemblyName}-ben az IModule implementációnak 'Module' végződésű névvel kell rendelkeznie");
    }
}
