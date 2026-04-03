using System.Reflection;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using FluentAssertions;
using MediatR;
using FluentValidation;
using Carter;

namespace BudgetBuddy.API.VSA.UnitTests.Architecture;

/// <summary>
/// Convention tests — every Handler, Validator and Domain Service must have
/// a corresponding *Tests class somewhere in this test assembly.
///
/// When you add a new Handler/Validator/Service and forget to write tests,
/// these tests fail and tell you exactly what is missing.
/// </summary>
public class TestCoverageConventionTests
{
    private static readonly Assembly MainAssembly = typeof(Program).Assembly;
    private static readonly IReadOnlySet<string> TestClassNames = MainAssembly
        .GetReferencedAssemblies()
        .Select(Assembly.Load)
        .Append(typeof(TestCoverageConventionTests).Assembly)
        .SelectMany(a => a.GetTypes())
        .Where(t => t.Name.EndsWith("Tests") && t.IsClass && !t.IsAbstract)
        .Select(t => t.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void AllHandlers_MustHaveTestClass()
    {
        var missing = MainAssembly.GetTypes()
            .Where(IsConcreteHandler)
            .Where(t => !HasTestClass(t))
            .Select(t => t.FullName!)
            .OrderBy(x => x)
            .ToList();

        missing.Should().BeEmpty(
            $"\n\nThe following MediatR handlers have no *Tests class:\n" +
            string.Join("\n", missing.Select(m => $"  - {m}")));
    }

    [Fact]
    public void AllValidators_MustHaveTestClass()
    {
        var missing = MainAssembly.GetTypes()
            .Where(IsConcreteValidator)
            .Where(t => !HasTestClass(t))
            .Select(t => t.FullName!)
            .OrderBy(x => x)
            .ToList();

        missing.Should().BeEmpty(
            $"\n\nThe following validators have no *Tests class:\n" +
            string.Join("\n", missing.Select(m => $"  - {m}")));
    }

    [Fact]
    public void AllDomainServices_MustHaveTestClass()
    {
        var missing = MainAssembly.GetTypes()
            .Where(IsDomainService)
            .Where(t => !HasTestClass(t))
            .Select(t => t.FullName!)
            .OrderBy(x => x)
            .ToList();

        missing.Should().BeEmpty(
            $"\n\nThe following domain services have no *Tests class:\n" +
            string.Join("\n", missing.Select(m => $"  - {m}")));
    }

    // Endpoints are thin Carter modules that delegate to MediatR — covered by integration tests.
    // See BudgetBuddy.API.VSA.IntegrationTests for endpoint-level coverage.

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool HasTestClass(Type type) =>
        TestClassNames.Contains($"{type.Name}Tests");

    private static bool IsConcreteHandler(Type t) =>
        t is { IsClass: true, IsAbstract: false } &&
        t.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

    private static bool IsConcreteValidator(Type t) =>
        t is { IsClass: true, IsAbstract: false } &&
        IsSubclassOfRawGeneric(typeof(AbstractValidator<>), t);

    private static bool IsDomainService(Type t) =>
        t is { IsClass: true, IsAbstract: false } &&
        // Has at least one interface named I*Service
        t.GetInterfaces().Any(i => i.Name.StartsWith('I') && i.Name.EndsWith("Service")) &&
        // Exclude infrastructure/stub implementations — only Feature services
        t.Namespace?.Contains(".Features.") == true;

    private static bool IsEndpoint(Type t) =>
        t is { IsClass: true, IsAbstract: false } &&
        typeof(ICarterModule).IsAssignableFrom(t);

    private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        var current = toCheck.BaseType;
        while (current is not null && current != typeof(object))
        {
            var cur = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (generic == cur) return true;
            current = current.BaseType;
        }
        return false;
    }
}
