using System.Reflection;
using FluentAssertions;
using MediatR;
using BudgetBuddy.Shared.Contracts.Events;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Cross-modul kommunikáció csak Shared.Contracts-on keresztül mehet
/// (mmrule.md – 4. Cross-module kommunikáció).
/// </summary>
public class CrossModuleCommunicationTests
{

    /// <summary>
    /// Az INotificationHandler<T> implementációkban a T típusnak IDomainEvent-et kell
    /// implementálnia — azaz csak Shared.Contracts.Events-ből jövő eseményeket kezelhet
    /// (a forrás modul nem tud a fogyasztóról).
    /// </summary>
    [Fact]
    public void NotificationHandlers_ShouldOnlyHandleDomainEvents()
    {
        var violations = new List<string>();

        foreach (var assembly in Assemblies.AllModules)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false }
                            && ImplementsGenericInterface(t, typeof(INotificationHandler<>)))
                .ToList();

            foreach (var handler in handlerTypes)
            {
                var eventTypes = handler.GetInterfaces()
                    .Where(i => i.IsGenericType
                                && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(i => i.GetGenericArguments()[0]);

                foreach (var eventType in eventTypes)
                {
                    if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
                    {
                        violations.Add(
                            $"{handler.FullName} → kezeli: {eventType.FullName} (nem IDomainEvent)");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            because: $"INotificationHandler-ek csak IDomainEvent implementációkat kezelhetnek " +
                     $"(ezek Shared.Contracts.Events-ben vannak definiálva). Sértők:\n" +
                     string.Join("\n", violations));
    }

    /// <summary>
    /// Ha egy modul Service osztálya külső publikus interface-t implementál,
    /// az interface NEM lehet egy másik modul belső interface-e.
    /// Modul-belső interface implementálása (saját névtéren belül) teljesen rendben van.
    /// </summary>
    [Fact]
    public void ModuleServices_ShouldNotImplementOtherModulesInternalInterfaces()
    {
        var violations = new List<string>();

        foreach (var assembly in Assemblies.AllModules)
        {
            var moduleRootNs = GetModuleRootNamespace(assembly);

            var serviceTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false }
                            && (t.Name.EndsWith("Service", StringComparison.Ordinal)
                                || t.Namespace?.Contains(".Services", StringComparison.Ordinal) == true))
                .ToList();

            foreach (var service in serviceTypes)
            {
                // Csak azokat az interface-eket vizsgáljuk, amelyek más modul névterében vannak
                var otherModuleInterfaces = service.GetInterfaces()
                    .Where(i => i.Namespace is not null
                                && i.Namespace.StartsWith("BudgetBuddy.Module.", StringComparison.Ordinal)
                                && !i.Namespace.StartsWith(moduleRootNs, StringComparison.Ordinal))
                    .ToList();

                foreach (var iface in otherModuleInterfaces)
                {
                    violations.Add($"{service.FullName} → implementálja: {iface.FullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            because: $"Modul Service osztályai nem implementálhatnak más modul belső interface-ét — " +
                     $"ez közvetlen csatolást jelent. Csak Shared.Contracts interface-ek megengedettek. " +
                     $"Sértők:\n{string.Join("\n", violations)}");
    }

    private static string GetModuleRootNamespace(Assembly assembly)
    {
        return assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns?.StartsWith("BudgetBuddy.Module.", StringComparison.Ordinal) == true)
            .Select(ns => string.Join(".", ns!.Split('.').Take(3)))
            .FirstOrDefault() ?? assembly.GetName().Name ?? "Unknown";
    }

    /// <summary>
    /// Ellenőrzi, hogy a Shared.Contracts.Events-ben definiált eseményeket
    /// tényleg a forrás moduljukon kívüli modulok is kezelik (event-driven kommunikáció működik).
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldHaveAtLeastOneHandler()
    {
        var domainEventTypes = Assemblies.SharedContracts.GetTypes()
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t)
                        && t is { IsInterface: false, IsAbstract: false })
            .ToList();

        // Ha nincs egyetlen event sem definiálva, a teszt nem releváns
        if (domainEventTypes.Count == 0) return;

        var allHandlerEventTypes = Assemblies.AllModules
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && ImplementsGenericInterface(t, typeof(INotificationHandler<>)))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => i.GetGenericArguments()[0]))
            .ToHashSet();

        var unhandledEvents = domainEventTypes
            .Where(e => !allHandlerEventTypes.Contains(e))
            .Select(e => e.FullName!)
            .ToList();

        unhandledEvents.Should().BeEmpty(
            because: $"Minden IDomainEvent-nek legalább egy INotificationHandler implementációval kell rendelkeznie. " +
                     $"Esemény handler nélkül: {string.Join(", ", unhandledEvents)}");
    }

    // --- helpers ---

    private static bool ImplementsGenericInterface(Type type, Type genericInterfaceDefinition) =>
        type.GetInterfaces()
            .Any(i => i.IsGenericType
                      && i.GetGenericTypeDefinition() == genericInterfaceDefinition);
}
