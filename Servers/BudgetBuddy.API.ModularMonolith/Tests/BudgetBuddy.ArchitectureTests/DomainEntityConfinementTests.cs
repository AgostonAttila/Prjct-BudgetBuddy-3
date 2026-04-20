using System.Reflection;
using FluentAssertions;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Domain entitás határ tesztek: a .Domain. névtérben lévő típusok
/// nem léphetnek ki a saját moduljukból (mmrule.md – 6. Module public API).
/// </summary>
public class DomainEntityConfinementTests
{
    /// <summary>
    /// Minden modul összes Domain entitása csak a saját moduljában jelenik meg
    /// más modulok dependency-listájában.
    ///
    /// Ezt részben a ModuleIsolationTests már lefedi (ha modul X nem hivatkozhat
    /// modul Y namespace-ére, akkor Y.Domain típusaira sem), de ez a teszt
    /// explicit és jobb hibaüzenetet ad.
    /// </summary>
    [Fact]
    public void DomainTypes_ShouldNotBeReferencedByOtherModules()
    {
        // Összegyűjtjük az összes modul Domain névterének típusait
        var domainTypesByModule = Assemblies.AllModules
            .Select(assembly => new
            {
                Assembly = assembly,
                ModuleNamespace = GetModuleRootNamespace(assembly),
                DomainTypes = GetDomainTypes(assembly)
            })
            .Where(m => m.DomainTypes.Count > 0)
            .ToList();

        var violations = new List<string>();

        foreach (var source in domainTypesByModule)
        {
            // Ellenőrizzük, hogy más modulok nem hivatkoznak source domain típusaira
            foreach (var target in Assemblies.AllModules)
            {
                if (target == source.Assembly) continue;

                var targetNamespace = GetModuleRootNamespace(target);
                var referencedDomainTypes = GetDomainTypesReferencedFromAssembly(
                    source.DomainTypes, target);

                foreach (var (domainType, referencingType) in referencedDomainTypes)
                {
                    violations.Add(
                        $"[{targetNamespace}] {referencingType.Name} → " +
                        $"[{source.ModuleNamespace}.Domain] {domainType.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            because: $"Domain entitások nem hagyhatják el a saját moduljukat. Jogsértések:\n" +
                     string.Join("\n", violations));
    }

    private static List<Type> GetDomainTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true
                        && t is { IsClass: true, IsAbstract: false })
            .ToList();
    }

    private static List<(Type domainType, Type referencingType)> GetDomainTypesReferencedFromAssembly(
        List<Type> domainTypes, Assembly targetAssembly)
    {
        var result = new List<(Type, Type)>();
        var targetTypes = targetAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var targetType in targetTypes)
        {
            var referencedTypes = GetAllReferencedTypes(targetType);
            foreach (var domainType in domainTypes)
            {
                if (referencedTypes.Contains(domainType))
                    result.Add((domainType, targetType));
            }
        }

        return result;
    }

    private static HashSet<Type> GetAllReferencedTypes(Type type)
    {
        var result = new HashSet<Type>();

        // Base class
        if (type.BaseType is not null) result.Add(type.BaseType);

        // Interfaces
        foreach (var iface in type.GetInterfaces())
            result.Add(iface);

        // Fields
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            result.Add(field.FieldType);

        // Properties
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            result.Add(prop.PropertyType);

        // Method parameters and return types
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            result.Add(method.ReturnType);
            foreach (var param in method.GetParameters())
                result.Add(param.ParameterType);
        }

        // Constructor parameters
        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var param in ctor.GetParameters())
                result.Add(param.ParameterType);
        }

        return result;
    }

    private static string GetModuleRootNamespace(Assembly assembly)
    {
        // pl. BudgetBuddy.Module.Accounts.AccountsModule → BudgetBuddy.Module.Accounts
        return assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns?.StartsWith("BudgetBuddy.Module.", StringComparison.Ordinal) == true)
            .Select(ns => string.Join(".", ns!.Split('.').Take(3)))
            .FirstOrDefault() ?? assembly.GetName().Name ?? "Unknown";
    }
}
