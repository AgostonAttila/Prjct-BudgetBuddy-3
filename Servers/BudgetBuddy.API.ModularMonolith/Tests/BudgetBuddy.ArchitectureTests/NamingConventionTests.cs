using System.Reflection;
using FluentAssertions;
using MediatR;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Elnevezési konvenciók az összes modul assembly-jében
/// (mmrule.md – 5. Projekt struktúra).
/// </summary>
public class NamingConventionTests
{
    private static readonly Type[] AllModuleTypes =
        Assemblies.AllModules
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: false })
            .ToArray();

    // -------------------------------------------------------------------
    // Handler
    // -------------------------------------------------------------------

    [Fact]
    public void ClassesEndingWithHandler_ShouldImplementIRequestHandlerOrINotificationHandler()
    {
        // Csak a .Features. névtérben lévő Handler-eket ellenőrizzük.
        // Az Infrastructure.Authorization namespace-ben lévők ASP.NET Core IAuthorizationHandler-ek —
        // ezek más konvencióra épülnek és nem MediatR-t használnak.
        var handlerTypes = AllModuleTypes
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal)
                        && t.Namespace?.Contains(".Features.", StringComparison.Ordinal) == true)
            .ToList();

        var violations = handlerTypes
            .Where(t => !ImplementsGenericInterface(t, typeof(IRequestHandler<,>))
                        && !ImplementsGenericInterface(t, typeof(INotificationHandler<>)))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            because: $"A 'Handler' végű osztályoknak IRequestHandler<,> vagy INotificationHandler<> interfészt kell implementálniuk. " +
                     $"Sértők: {string.Join(", ", violations)}");
    }

    // -------------------------------------------------------------------
    // Command / Query
    // -------------------------------------------------------------------

    [Fact]
    public void ClassesEndingWithCommand_ShouldImplementIRequest()
    {
        var commandTypes = AllModuleTypes
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal))
            .ToList();

        var violations = commandTypes
            .Where(t => !ImplementsGenericInterface(t, typeof(IRequest<>))
                        && !typeof(IRequest).IsAssignableFrom(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            because: $"A 'Command' végű osztályoknak IRequest vagy IRequest<T> interfészt kell implementálniuk. " +
                     $"Sértők: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ClassesEndingWithQuery_ShouldImplementIRequest()
    {
        var queryTypes = AllModuleTypes
            .Where(t => t.Name.EndsWith("Query", StringComparison.Ordinal))
            .ToList();

        var violations = queryTypes
            .Where(t => !ImplementsGenericInterface(t, typeof(IRequest<>))
                        && !typeof(IRequest).IsAssignableFrom(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            because: $"A 'Query' végű osztályoknak IRequest<T> interfészt kell implementálniuk. " +
                     $"Sértők: {string.Join(", ", violations)}");
    }

    // -------------------------------------------------------------------
    // Validator
    // -------------------------------------------------------------------

    [Fact]
    public void ClassesEndingWithValidator_ShouldInheritAbstractValidator()
    {
        var validatorTypes = AllModuleTypes
            .Where(t => t.Name.EndsWith("Validator", StringComparison.Ordinal))
            .ToList();

        var violations = validatorTypes
            .Where(t => !InheritsFromAbstractValidator(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            because: $"A 'Validator' végű osztályoknak FluentValidation AbstractValidator<T>-ből kell örökölniük. " +
                     $"Sértők: {string.Join(", ", violations)}");
    }

    // -------------------------------------------------------------------
    // Features namespace check
    // -------------------------------------------------------------------

    [Fact]
    public void CommandsAndQueries_ShouldResideInFeaturesNamespace()
    {
        var cqTypes = AllModuleTypes
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal)
                        || t.Name.EndsWith("Query", StringComparison.Ordinal))
            .ToList();

        var violations = cqTypes
            .Where(t => t.Namespace is null || !t.Namespace.Contains(".Features.", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            because: $"Command és Query osztályok csak '.Features.' namespace-ben lehetnek. " +
                     $"Sértők: {string.Join(", ", violations)}");
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static bool ImplementsGenericInterface(Type type, Type genericInterfaceDefinition)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType
                      && i.GetGenericTypeDefinition() == genericInterfaceDefinition);
    }

    private static bool InheritsFromAbstractValidator(Type type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition().FullName ==
                   "FluentValidation.AbstractValidator`1")
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
