using FluentAssertions;
using NetArchTest.Rules;

namespace BudgetBuddy.ArchitectureTests;

/// <summary>
/// Modul izolációs tesztek: egyetlen modul sem hivatkozhat közvetlenül
/// egy másik modul belső típusaira (mmrule.md – 1. Modul határok).
/// </summary>
public class ModuleIsolationTests
{
    [Fact]
    public void Accounts_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Accounts)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Accounts"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Accounts modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Analytics_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Analytics)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Analytics"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Analytics modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Auth_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Auth)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Auth"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Auth modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Budgets_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Budgets)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Budgets"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Budgets modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Investments_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Investments)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Investments"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Investments modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void ReferenceData_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.ReferenceData)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.ReferenceData"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"ReferenceData modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Transactions_ShouldNotDependOnOtherModules()
    {
        var result = Types.InAssembly(Assemblies.Transactions)
            .Should()
            .NotHaveDependencyOnAny(OtherModuleNamespaces("BudgetBuddy.Module.Transactions"))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"Transactions modul nem hivatkozhat más modulok belső típusaira. Sértő típusok: {FormatFailingTypes(result)}");
    }

    // --- helpers ---

    private static string[] OtherModuleNamespaces(string ownNamespace) =>
        Assemblies.AllModuleNamespaces
            .Where(ns => ns != ownNamespace)
            .ToArray();

    private static string FormatFailingTypes(TestResult result) =>
        result.FailingTypeNames is null
            ? "(nincs adat)"
            : string.Join(", ", result.FailingTypeNames);
}
