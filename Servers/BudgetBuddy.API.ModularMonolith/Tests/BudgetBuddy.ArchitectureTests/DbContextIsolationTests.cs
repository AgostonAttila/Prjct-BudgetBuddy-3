using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Minden modul assembly-ben pontosan egy DbContext leszármazott van,
/// és a Shared rétegek nem tartalmaznak modul-specifikus DbContext-et
/// (mmrule.md – 2. Adat izoláció).
/// </summary>
public class DbContextIsolationTests
{
    public static IEnumerable<object[]> ModuleAssemblies() =>
        Assemblies.AllModules.Select(a => new object[] { a.GetName().Name!, a });

    /// <summary>
    /// Egy modul legfeljebb 1 DbContext-et tartalmazhat.
    /// 0 DbContext megengedett (pl. Analytics — read-only modul, contract interface-eken olvassa az adatot).
    /// 2+ DbContext sértenné az adat izolációt.
    /// </summary>
    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void EachModule_ShouldHaveAtMostOneDbContext(string assemblyName, Assembly assembly)
    {
        var dbContextTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && IsDbContextSubclass(t))
            .ToList();

        dbContextTypes.Should().HaveCountLessOrEqualTo(1,
            because: $"{assemblyName} nem tartalmazhat 1-nél több DbContext-et " +
                     $"(adat izoláció). Talált: {string.Join(", ", dbContextTypes.Select(t => t.Name))}");
    }

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void EachModuleDbContext_ShouldBeNamedAfterModule(string assemblyName, Assembly assembly)
    {
        var dbContextTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsDbContextSubclass(t))
            .ToList();

        // Read-only modulok (pl. Analytics) nem rendelkeznek DbContext-tel — ez rendben van
        if (dbContextTypes.Count == 0) return;

        dbContextTypes.Single().Name.Should().EndWith("DbContext",
            because: $"{assemblyName} DbContext osztályának 'DbContext' végződéssel kell rendelkeznie");
    }

    [Fact]
    public void SharedKernel_ShouldNotContainDbContext()
    {
        var dbContextTypes = Assemblies.SharedKernel.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsDbContextSubclass(t))
            .ToList();

        dbContextTypes.Should().BeEmpty(
            because: "Shared.Kernel nem tartalmazhat DbContext-et — ez megsértené a modul adat izolációt");
    }

    [Fact]
    public void SharedContracts_ShouldNotContainDbContext()
    {
        var dbContextTypes = Assemblies.SharedContracts.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsDbContextSubclass(t))
            .ToList();

        dbContextTypes.Should().BeEmpty(
            because: "Shared.Contracts nem tartalmazhat DbContext-et");
    }

    private static bool IsDbContextSubclass(Type t)
    {
        var current = t.BaseType;
        while (current is not null)
        {
            if (current == typeof(DbContext)) return true;
            current = current.BaseType;
        }
        return false;
    }
}
